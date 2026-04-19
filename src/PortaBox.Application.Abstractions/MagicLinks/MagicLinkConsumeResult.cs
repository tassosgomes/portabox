namespace PortaBox.Application.Abstractions.MagicLinks;

public sealed record MagicLinkConsumeResult(
    MagicLinkConsumeStatus Status,
    Guid? MagicLinkId,
    Guid? UserId,
    MagicLinkPurpose Purpose,
    MagicLinkFailureReason FailureReason)
{
    public bool IsSuccess => Status == MagicLinkConsumeStatus.Consumed;

    public static MagicLinkConsumeResult Consumed(Guid magicLinkId, Guid userId, MagicLinkPurpose purpose)
    {
        return new MagicLinkConsumeResult(
            MagicLinkConsumeStatus.Consumed,
            magicLinkId,
            userId,
            purpose,
            MagicLinkFailureReason.None);
    }

    public static MagicLinkConsumeResult Invalid(MagicLinkPurpose purpose, MagicLinkFailureReason failureReason)
    {
        return new MagicLinkConsumeResult(
            MagicLinkConsumeStatus.Invalid,
            null,
            null,
            purpose,
            failureReason);
    }
}
