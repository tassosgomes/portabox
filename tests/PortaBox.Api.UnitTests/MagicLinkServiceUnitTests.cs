using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PortaBox.Application.Abstractions.MagicLinks;
using PortaBox.Application.Abstractions.Observability;
using PortaBox.Infrastructure.MagicLinks;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Api.UnitTests;

public sealed class MagicLinkServiceUnitTests
{
    [Fact]
    public async Task IssueAsync_ShouldReturnBase64UrlTokenAndPersistSha256HexHash()
    {
        await using var dbContext = BuildDbContext();
        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 4, 18, 12, 0, 0, TimeSpan.Zero));
        var service = CreateService(dbContext, timeProvider);

        var result = await service.IssueAsync(Guid.NewGuid(), MagicLinkPurpose.PasswordSetup);
        var persisted = await dbContext.MagicLinks.SingleAsync();
        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(result.RawToken!))).ToLowerInvariant();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.RawToken);
        Assert.Equal(43, result.RawToken!.Length);
        Assert.Matches("^[A-Za-z0-9_-]{43}$", result.RawToken);
        Assert.NotNull(result.TokenHash);
        Assert.Equal(64, result.TokenHash!.Length);
        Assert.Matches("^[0-9a-f]{64}$", result.TokenHash);
        Assert.Equal(expectedHash, result.TokenHash);
        Assert.Equal(expectedHash, persisted.TokenHash);
    }

    [Fact]
    public async Task IssueAsync_ShouldStoreHashComputedFromUtf8TokenUsingSha256()
    {
        const string knownValue = "magic-link-known-vector";
        const string knownHash = "f6d48485e4890d53bdaf45fe7cc789cd926be836131d8931ac4cc45507ddffcb";

        var expectedKnownHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(knownValue))).ToLowerInvariant();
        Assert.Equal(knownHash, expectedKnownHash);

        await using var dbContext = BuildDbContext();
        var service = CreateService(dbContext, new MutableTimeProvider(DateTimeOffset.UtcNow));

        var result = await service.IssueAsync(Guid.NewGuid(), MagicLinkPurpose.PasswordSetup);
        var persistedHash = await dbContext.MagicLinks.Select(magicLink => magicLink.TokenHash).SingleAsync();
        var recomputedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(result.RawToken!))).ToLowerInvariant();

        Assert.Equal(recomputedHash, persistedHash);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_ShouldReturnSuccessOnlyWhenLinkIsPending()
    {
        var now = new DateTimeOffset(2026, 4, 18, 12, 0, 0, TimeSpan.Zero);

        await using (var successContext = BuildDbContext())
        {
            var service = CreateService(successContext, new MutableTimeProvider(now));
            var issued = await service.IssueAsync(Guid.NewGuid(), MagicLinkPurpose.PasswordSetup);

            var result = await service.ValidateAndConsumeAsync(issued.RawToken!, MagicLinkPurpose.PasswordSetup);
            var persisted = await successContext.MagicLinks.SingleAsync();

            Assert.True(result.IsSuccess);
            Assert.NotNull(persisted.ConsumedAt);
        }

        await using (var expiredContext = BuildDbContext())
        {
            var rawToken = "expired-token";
            expiredContext.MagicLinks.Add(MagicLink.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                MagicLinkPurpose.PasswordSetup,
                ComputeHash(rawToken),
                now.AddDays(-4),
                now.AddMinutes(-1)));
            await expiredContext.SaveChangesAsync();

            var service = CreateService(expiredContext, new MutableTimeProvider(now));
            var result = await service.ValidateAndConsumeAsync(rawToken, MagicLinkPurpose.PasswordSetup);

            Assert.False(result.IsSuccess);
            Assert.Equal(MagicLinkFailureReason.Expired, result.FailureReason);
        }

        await using (var invalidatedContext = BuildDbContext())
        {
            var rawToken = "invalidated-token";
            var magicLink = MagicLink.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                MagicLinkPurpose.PasswordSetup,
                ComputeHash(rawToken),
                now,
                now.AddHours(2));
            magicLink.MarkInvalidated(now.AddMinutes(1));
            invalidatedContext.MagicLinks.Add(magicLink);
            await invalidatedContext.SaveChangesAsync();

            var service = CreateService(invalidatedContext, new MutableTimeProvider(now.AddMinutes(2)));
            var result = await service.ValidateAndConsumeAsync(rawToken, MagicLinkPurpose.PasswordSetup);

            Assert.False(result.IsSuccess);
            Assert.Equal(MagicLinkFailureReason.Invalidated, result.FailureReason);
        }
    }

    [Fact]
    public async Task IssueAsync_ShouldReturnRateLimitedOnSixthIssuanceWithinWindow()
    {
        await using var dbContext = BuildDbContext();
        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 4, 18, 12, 0, 0, TimeSpan.Zero));
        var service = CreateService(dbContext, timeProvider);
        var userId = Guid.NewGuid();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var result = await service.IssueAsync(userId, MagicLinkPurpose.PasswordSetup);
            Assert.True(result.IsSuccess);
            timeProvider.Advance(TimeSpan.FromMinutes(1));
        }

        var rateLimited = await service.IssueAsync(userId, MagicLinkPurpose.PasswordSetup);

        Assert.False(rateLimited.IsSuccess);
        Assert.Equal(MagicLinkIssueStatus.RateLimited, rateLimited.Status);
        Assert.Equal(MagicLinkFailureReason.RateLimited, rateLimited.FailureReason);
        Assert.NotNull(rateLimited.RetryAfter);
    }

    [Fact]
    public async Task IssueAsync_ShouldLogWithoutExposingRawToken()
    {
        await using var dbContext = BuildDbContext();
        var sink = new ListLoggerSink();
        var logger = new ListLogger<MagicLinkService>(sink);
        var service = CreateService(dbContext, new MutableTimeProvider(DateTimeOffset.UtcNow), logger);

        var result = await service.IssueAsync(Guid.NewGuid(), MagicLinkPurpose.PasswordSetup);

        var entry = Assert.Single(sink.Entries, logEntry => logEntry.Message.Contains("magic_link.issued", StringComparison.Ordinal));
        Assert.NotNull(result.RawToken);
        Assert.DoesNotContain(result.RawToken!, entry.Message, StringComparison.Ordinal);
        Assert.True(entry.Properties.ContainsKey("token_id"));
        Assert.True(entry.Properties.ContainsKey("token_hash"));
    }

    private static AppDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private static MagicLinkService CreateService(
        AppDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<MagicLinkService>? logger = null)
    {
        return new MagicLinkService(
            dbContext,
            Microsoft.Extensions.Options.Options.Create(new MagicLinkOptions()),
            logger ?? NullLogger<MagicLinkService>.Instance,
            new NoOpGestaoMetrics(),
            timeProvider);
    }

    private static string ComputeHash(string rawToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
    }

    private sealed class ListLoggerSink
    {
        public List<ListLogEntry> Entries { get; } = [];
    }

    private sealed record ListLogEntry(LogLevel Level, string Message, IReadOnlyDictionary<string, object?> Properties);

    private sealed class ListLogger<T>(ListLoggerSink sink) : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state is IEnumerable<KeyValuePair<string, object?>> pairs
                ? pairs.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
                : new Dictionary<string, object?>(StringComparer.Ordinal);

            sink.Entries.Add(new ListLogEntry(logLevel, formatter(state, exception), properties));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class NoOpGestaoMetrics : IGestaoMetrics
    {
        public void IncrementCondominioActivated() { }
        public void IncrementCondominioCreated(string statusOutcome = "success") { }
        public void IncrementMagicLinkConsumed(string purpose) { }
        public void IncrementMagicLinkExpired(string purpose) { }
        public void IncrementMagicLinkIssued(string purpose) { }
        public void RecordEmailSendDuration(TimeSpan duration, string template, string outcome) { }
        public void UpdateDomainEventOutboxPendingCount(long pendingCount) { }
        public void UpdateEmailOutboxAge(double oldestAgeSeconds) { }
    }
}
