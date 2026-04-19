namespace PortaBox.Application.Abstractions.MagicLinks;

public sealed record MagicLinkIssueResult(
    MagicLinkIssueStatus Status,
    Guid? MagicLinkId,
    Guid UserId,
    MagicLinkPurpose Purpose,
    string? RawToken,
    string? TokenHash,
    DateTimeOffset? ExpiresAt,
    MagicLinkFailureReason FailureReason,
    DateTimeOffset? RetryAfter)
{
    public bool IsSuccess => Status == MagicLinkIssueStatus.Issued;

    public static MagicLinkIssueResult Issued(
        Guid magicLinkId,
        Guid userId,
        MagicLinkPurpose purpose,
        string rawToken,
        string tokenHash,
        DateTimeOffset expiresAt)
    {
        return new MagicLinkIssueResult(
            MagicLinkIssueStatus.Issued,
            magicLinkId,
            userId,
            purpose,
            rawToken,
            tokenHash,
            expiresAt,
            MagicLinkFailureReason.None,
            null);
    }

    public static MagicLinkIssueResult RateLimited(
        Guid userId,
        MagicLinkPurpose purpose,
        DateTimeOffset retryAfter)
    {
        return new MagicLinkIssueResult(
            MagicLinkIssueStatus.RateLimited,
            null,
            userId,
            purpose,
            null,
            null,
            null,
            MagicLinkFailureReason.RateLimited,
            retryAfter);
    }
}
