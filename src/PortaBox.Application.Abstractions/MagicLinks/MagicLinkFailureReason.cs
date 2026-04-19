namespace PortaBox.Application.Abstractions.MagicLinks;

public enum MagicLinkFailureReason
{
    None = 0,
    NotFound = 1,
    Expired = 2,
    AlreadyConsumed = 3,
    Invalidated = 4,
    RateLimited = 5
}
