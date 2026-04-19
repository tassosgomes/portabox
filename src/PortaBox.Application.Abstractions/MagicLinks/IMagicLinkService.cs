namespace PortaBox.Application.Abstractions.MagicLinks;

public interface IMagicLinkService
{
    Task<MagicLinkIssueResult> CanIssueAsync(
        Guid userId,
        MagicLinkPurpose purpose,
        CancellationToken ct = default);

    Task<MagicLinkIssueResult> IssueAsync(
        Guid userId,
        MagicLinkPurpose purpose,
        TimeSpan? ttl = null,
        CancellationToken ct = default);

    Task<MagicLinkConsumeResult> ValidateAndConsumeAsync(
        string rawToken,
        MagicLinkPurpose purpose,
        CancellationToken ct = default);

    Task InvalidatePendingAsync(Guid userId, MagicLinkPurpose purpose, CancellationToken ct = default);
}
