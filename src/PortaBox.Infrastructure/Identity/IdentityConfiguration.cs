namespace PortaBox.Infrastructure.Identity;

public sealed class IdentityConfiguration
{
    public const string SectionName = "Identity";

    public PasswordPolicyConfiguration Password { get; set; } = new();

    public DevelopmentOperatorConfiguration DevelopmentOperator { get; set; } = new();
}

public sealed class PasswordPolicyConfiguration
{
    public int RequiredLength { get; set; } = 10;

    public int RequiredUniqueChars { get; set; } = 1;

    public bool RequireDigit { get; set; } = true;

    public bool RequireLowercase { get; set; }

    public bool RequireUppercase { get; set; }

    public bool RequireNonAlphanumeric { get; set; }
}

public sealed class DevelopmentOperatorConfiguration
{
    public string Email { get; set; } = "operator@portabox.dev";

    public string Password { get; set; } = "PortaBox123!";
}
