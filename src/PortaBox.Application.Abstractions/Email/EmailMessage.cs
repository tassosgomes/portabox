namespace PortaBox.Application.Abstractions.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? TextBody = null,
    IReadOnlyDictionary<string, string>? Tags = null);
