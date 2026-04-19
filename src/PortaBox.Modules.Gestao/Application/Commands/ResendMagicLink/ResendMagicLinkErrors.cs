namespace PortaBox.Modules.Gestao.Application.Commands.ResendMagicLink;

public static class ResendMagicLinkErrors
{
    public const string NotFound = "NotFound";
    public const string AlreadyHasPassword = "AlreadyHasPassword";
    public const string RateLimited = "RateLimited";
    public const string MagicLinkIssueFailed = "MagicLinkIssueFailed";
}
