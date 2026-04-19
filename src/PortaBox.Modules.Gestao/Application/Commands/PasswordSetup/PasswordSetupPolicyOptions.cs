namespace PortaBox.Modules.Gestao.Application.Commands.PasswordSetup;

public sealed class PasswordSetupPolicyOptions
{
    public const string SectionName = "Identity:Password";

    public int RequiredLength { get; set; } = 10;

    public bool RequireDigit { get; set; } = true;

    public bool RequireLowercase { get; set; }

    public bool RequireUppercase { get; set; }

    public bool RequireNonAlphanumeric { get; set; }
}
