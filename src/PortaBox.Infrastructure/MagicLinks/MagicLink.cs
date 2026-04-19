using System.Net;
using PortaBox.Application.Abstractions.MagicLinks;

namespace PortaBox.Infrastructure.MagicLinks;

public sealed class MagicLink
{
    private MagicLink()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public MagicLinkPurpose Purpose { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public IPAddress? ConsumedByIp { get; private set; }

    public DateTimeOffset? InvalidatedAt { get; private set; }

    public static MagicLink Create(
        Guid id,
        Guid userId,
        MagicLinkPurpose purpose,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        return new MagicLink
        {
            Id = id,
            UserId = userId,
            Purpose = purpose,
            TokenHash = tokenHash.Trim(),
            CreatedAt = createdAt,
            ExpiresAt = expiresAt
        };
    }

    public bool IsPendingAt(DateTimeOffset now)
    {
        return ConsumedAt is null && InvalidatedAt is null && ExpiresAt > now;
    }

    public void MarkConsumed(DateTimeOffset consumedAt, IPAddress? consumedByIp)
    {
        ConsumedAt = consumedAt;
        ConsumedByIp = consumedByIp;
    }

    public void MarkInvalidated(DateTimeOffset invalidatedAt)
    {
        if (ConsumedAt is not null || InvalidatedAt is not null)
        {
            return;
        }

        InvalidatedAt = invalidatedAt;
    }
}
