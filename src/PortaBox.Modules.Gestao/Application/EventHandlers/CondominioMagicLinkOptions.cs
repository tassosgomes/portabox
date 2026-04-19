namespace PortaBox.Modules.Gestao.Application.EventHandlers;

public sealed class CondominioMagicLinkOptions
{
    public const string SectionName = "CondominioMagicLink";

    public string SindicoAppBaseUrl { get; set; } = "http://localhost:5174";
}
