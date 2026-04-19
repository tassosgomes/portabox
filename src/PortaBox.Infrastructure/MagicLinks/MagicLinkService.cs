using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PortaBox.Application.Abstractions.MagicLinks;
using PortaBox.Application.Abstractions.Observability;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Infrastructure.MagicLinks;

public sealed class MagicLinkService(
    AppDbContext dbContext,
    IOptions<MagicLinkOptions> optionsAccessor,
    ILogger<MagicLinkService> logger,
    IGestaoMetrics metrics,
    TimeProvider timeProvider,
    IHttpContextAccessor? httpContextAccessor = null) : IMagicLinkService
{
    private readonly MagicLinkOptions _options = optionsAccessor?.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));

    public Task<MagicLinkIssueResult> CanIssueAsync(
        Guid userId,
        MagicLinkPurpose purpose,
        CancellationToken ct = default)
    {
        return EvaluateIssuanceAsync(userId, purpose, ct);
    }

    public async Task<MagicLinkIssueResult> IssueAsync(
        Guid userId,
        MagicLinkPurpose purpose,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
    {
        var issueEligibility = await EvaluateIssuanceAsync(userId, purpose, ct);
        if (!issueEligibility.IsSuccess)
        {
            return issueEligibility;
        }

        var now = timeProvider.GetUtcNow();
        var invalidatedCount = await InvalidatePendingCoreAsync(userId, purpose, now, ct);
        var rawToken = CreateRawToken();
        var tokenHash = ComputeTokenHash(rawToken);
        var expiresAt = now + (ttl ?? _options.DefaultTtl);
        var magicLink = MagicLink.Create(Guid.NewGuid(), userId, purpose, tokenHash, now, expiresAt);

        dbContext.MagicLinks.Add(magicLink);
        await dbContext.SaveChangesAsync(ct);
        metrics.IncrementMagicLinkIssued(purpose.ToString());

        logger.LogInformation(
            "Magic link issued. {event} user_id={user_id} token_id={token_id} purpose={purpose} expires_at={expires_at} token_hash={token_hash}",
            "magic_link.issued",
            userId,
            magicLink.Id,
            purpose,
            expiresAt,
            tokenHash);

        if (invalidatedCount > 0)
        {
            logger.LogInformation(
                "Magic links invalidated. {event} user_id={user_id} purpose={purpose} count={count}",
                "magic_link.invalidated",
                userId,
                purpose,
                invalidatedCount);
        }

        return MagicLinkIssueResult.Issued(magicLink.Id, userId, purpose, rawToken, tokenHash, expiresAt);
    }

    private async Task<MagicLinkIssueResult> EvaluateIssuanceAsync(
        Guid userId,
        MagicLinkPurpose purpose,
        CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var rateLimitWindowStart = now - _options.RateLimitWindow;
        var attemptsInWindow = await dbContext.MagicLinks
            .CountAsync(
                magicLink => magicLink.UserId == userId &&
                             magicLink.Purpose == purpose &&
                             magicLink.CreatedAt >= rateLimitWindowStart,
                ct);

        if (attemptsInWindow < _options.MaxIssuancesPerWindow)
        {
            return MagicLinkIssueResult.Issued(Guid.Empty, userId, purpose, string.Empty, string.Empty, now);
        }

        var retryAfter = await dbContext.MagicLinks
            .Where(magicLink => magicLink.UserId == userId && magicLink.Purpose == purpose && magicLink.CreatedAt >= rateLimitWindowStart)
            .OrderBy(magicLink => magicLink.CreatedAt)
            .Select(magicLink => magicLink.CreatedAt)
            .FirstAsync(ct);

        return MagicLinkIssueResult.RateLimited(userId, purpose, retryAfter + _options.RateLimitWindow);
    }

    public async Task<MagicLinkConsumeResult> ValidateAndConsumeAsync(
        string rawToken,
        MagicLinkPurpose purpose,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            logger.LogWarning(
                "Magic link consume failed. {event} purpose={purpose} reason_code={reason_code}",
                "magic_link.consume_failed",
                purpose,
                MagicLinkFailureReason.NotFound);

            return MagicLinkConsumeResult.Invalid(purpose, MagicLinkFailureReason.NotFound);
        }

        var now = timeProvider.GetUtcNow();
        var tokenHash = ComputeTokenHash(rawToken);
        var snapshot = await dbContext.MagicLinks
            .AsNoTracking()
            .Where(magicLink => magicLink.TokenHash == tokenHash && magicLink.Purpose == purpose)
            .Select(magicLink => new MagicLinkSnapshot(
                magicLink.Id,
                magicLink.UserId,
                magicLink.Purpose,
                magicLink.ExpiresAt,
                magicLink.ConsumedAt,
                magicLink.InvalidatedAt))
            .SingleOrDefaultAsync(ct);

        if (snapshot is null)
        {
            logger.LogWarning(
                "Magic link consume failed. {event} purpose={purpose} reason_code={reason_code} token_hash={token_hash}",
                "magic_link.consume_failed",
                purpose,
                MagicLinkFailureReason.NotFound,
                tokenHash);

            return MagicLinkConsumeResult.Invalid(purpose, MagicLinkFailureReason.NotFound);
        }

        var remoteIpAddress = httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress;
        var rowsAffected = await ConsumePendingCoreAsync(snapshot.Id, tokenHash, purpose, now, remoteIpAddress, ct);

        if (rowsAffected == 1)
        {
            metrics.IncrementMagicLinkConsumed(purpose.ToString());
            logger.LogInformation(
                "Magic link consumed. {event} user_id={user_id} token_id={token_id} purpose={purpose} ip={ip}",
                "magic_link.consumed",
                snapshot.UserId,
                snapshot.Id,
                purpose,
                remoteIpAddress);

            return MagicLinkConsumeResult.Consumed(snapshot.Id, snapshot.UserId, purpose);
        }

        var failureReason = ResolveFailureReason(snapshot, now);
        if (failureReason == MagicLinkFailureReason.Expired)
        {
            metrics.IncrementMagicLinkExpired(purpose.ToString());
        }

        logger.LogWarning(
            "Magic link consume failed. {event} token_id={token_id} purpose={purpose} reason_code={reason_code} token_hash={token_hash}",
            "magic_link.consume_failed",
            snapshot.Id,
            purpose,
            failureReason,
            tokenHash);

        return MagicLinkConsumeResult.Invalid(purpose, failureReason);
    }

    public async Task InvalidatePendingAsync(Guid userId, MagicLinkPurpose purpose, CancellationToken ct = default)
    {
        var invalidatedCount = await InvalidatePendingCoreAsync(userId, purpose, timeProvider.GetUtcNow(), ct);

        logger.LogInformation(
            "Magic links invalidated. {event} user_id={user_id} purpose={purpose} count={count}",
            "magic_link.invalidated",
            userId,
            purpose,
            invalidatedCount);
    }

    private async Task<int> InvalidatePendingCoreAsync(
        Guid userId,
        MagicLinkPurpose purpose,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var pendingQuery = dbContext.MagicLinks.Where(
            magicLink => magicLink.UserId == userId &&
                         magicLink.Purpose == purpose &&
                         magicLink.ConsumedAt == null &&
                         magicLink.InvalidatedAt == null &&
                         magicLink.ExpiresAt > now);

        if (dbContext.Database.IsRelational())
        {
            return await pendingQuery.ExecuteUpdateAsync(
                setters => setters.SetProperty(magicLink => magicLink.InvalidatedAt, now),
                ct);
        }

        var pendingLinks = await pendingQuery.ToListAsync(ct);
        foreach (var pendingLink in pendingLinks)
        {
            pendingLink.MarkInvalidated(now);
        }

        return await dbContext.SaveChangesAsync(ct);
    }

    private async Task<int> ConsumePendingCoreAsync(
        Guid magicLinkId,
        string tokenHash,
        MagicLinkPurpose purpose,
        DateTimeOffset now,
        IPAddress? remoteIpAddress,
        CancellationToken ct)
    {
        var pendingQuery = dbContext.MagicLinks.Where(
            magicLink => magicLink.Id == magicLinkId &&
                         magicLink.TokenHash == tokenHash &&
                         magicLink.Purpose == purpose &&
                         magicLink.ConsumedAt == null &&
                         magicLink.InvalidatedAt == null &&
                         magicLink.ExpiresAt > now);

        if (dbContext.Database.IsRelational())
        {
            return await pendingQuery.ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(magicLink => magicLink.ConsumedAt, now)
                    .SetProperty(magicLink => magicLink.ConsumedByIp, remoteIpAddress),
                ct);
        }

        var magicLink = await pendingQuery.SingleOrDefaultAsync(ct);
        if (magicLink is null)
        {
            return 0;
        }

        magicLink.MarkConsumed(now, remoteIpAddress);
        return await dbContext.SaveChangesAsync(ct);
    }

    private static MagicLinkFailureReason ResolveFailureReason(MagicLinkSnapshot snapshot, DateTimeOffset now)
    {
        if (snapshot.ConsumedAt is not null)
        {
            return MagicLinkFailureReason.AlreadyConsumed;
        }

        if (snapshot.InvalidatedAt is not null)
        {
            return MagicLinkFailureReason.Invalidated;
        }

        if (snapshot.ExpiresAt <= now)
        {
            return MagicLinkFailureReason.Expired;
        }

        return MagicLinkFailureReason.NotFound;
    }

    internal static string CreateRawToken()
    {
        Span<byte> tokenBytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(tokenBytes);
        return Convert.ToBase64String(tokenBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    internal static string ComputeTokenHash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private sealed record MagicLinkSnapshot(
        Guid Id,
        Guid UserId,
        MagicLinkPurpose Purpose,
        DateTimeOffset ExpiresAt,
        DateTimeOffset? ConsumedAt,
        DateTimeOffset? InvalidatedAt);
}
