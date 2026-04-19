namespace PortaBox.Application.Abstractions.MagicLinks;

public sealed class MagicLinkOptions
{
    public const string SectionName = "MagicLink";

    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromHours(72);

    public int MaxIssuancesPerWindow { get; set; } = 5;

    public TimeSpan RateLimitWindow { get; set; } = TimeSpan.FromHours(24);
}
