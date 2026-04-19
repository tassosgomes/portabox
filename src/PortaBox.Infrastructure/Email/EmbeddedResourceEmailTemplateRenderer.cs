using System.Reflection;
using PortaBox.Application.Abstractions.Email;

namespace PortaBox.Infrastructure.Email;

public sealed class EmbeddedResourceEmailTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly Assembly Assembly = typeof(EmbeddedResourceEmailTemplateRenderer).Assembly;
    private const string ResourcePrefix = "PortaBox.Infrastructure.Email.Resources.EmailTemplates.";

    public EmailTemplate Render(
        string templateName,
        IReadOnlyDictionary<string, string> variables)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentNullException.ThrowIfNull(variables);

        var htmlBody = ReplaceTokens(ReadResource($"{templateName}.html"), variables);
        var textBody = ReplaceTokens(ReadResource($"{templateName}.txt"), variables);
        return new EmailTemplate(htmlBody, textBody);
    }

    private static string ReadResource(string name)
    {
        var resourceName = $"{ResourcePrefix}{name}";
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Email template resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string ReplaceTokens(string content, IReadOnlyDictionary<string, string> variables)
    {
        var rendered = content;

        foreach (var variable in variables)
        {
            rendered = rendered.Replace(
                $"{{{variable.Key}}}",
                variable.Value,
                StringComparison.Ordinal);
        }

        return rendered;
    }
}
