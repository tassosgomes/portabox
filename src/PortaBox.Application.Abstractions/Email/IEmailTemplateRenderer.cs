namespace PortaBox.Application.Abstractions.Email;

public interface IEmailTemplateRenderer
{
    EmailTemplate Render(
        string templateName,
        IReadOnlyDictionary<string, string> variables);
}
