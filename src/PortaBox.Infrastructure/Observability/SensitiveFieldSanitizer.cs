using System.Collections;
using System.Text.RegularExpressions;

namespace PortaBox.Infrastructure.Observability;

public sealed partial class SensitiveFieldSanitizer
{
    private static readonly HashSet<string> SensitiveFieldNames =
    [
        "password",
        "rawtoken",
        "token",
        "cpf",
        "cnpj",
        "email"
    ];

    public object? Sanitize(string propertyName, object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (IsSensitiveField(propertyName))
        {
            return "***";
        }

        return value switch
        {
            string text => SanitizeText(text),
            IDictionary dictionary => SanitizeDictionary(dictionary),
            IEnumerable enumerable when value is not string => SanitizeSequence(propertyName, enumerable),
            _ => value
        };
    }

    public string SanitizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text ?? string.Empty;
        }

        var sanitized = JsonSensitiveFieldRegex().Replace(text, "$1***$3");
        sanitized = QueryStringSensitiveFieldRegex().Replace(sanitized, "$1***");
        sanitized = EmailRegex().Replace(sanitized, "***");
        sanitized = CpfRegex().Replace(sanitized, "***");
        sanitized = CnpjRegex().Replace(sanitized, "***");

        return sanitized;
    }

    private Dictionary<string, object?> SanitizeDictionary(IDictionary dictionary)
    {
        var sanitized = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (DictionaryEntry entry in dictionary)
        {
            var key = entry.Key?.ToString() ?? string.Empty;
            sanitized[key] = Sanitize(key, entry.Value);
        }

        return sanitized;
    }

    private object?[] SanitizeSequence(string propertyName, IEnumerable enumerable)
    {
        var items = new List<object?>();

        foreach (var item in enumerable)
        {
            items.Add(Sanitize(propertyName, item));
        }

        return items.ToArray();
    }

    private static bool IsSensitiveField(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return false;
        }

        var normalized = Normalize(propertyName);

        if (normalized.EndsWith("hash", StringComparison.Ordinal) ||
            normalized.EndsWith("suffix", StringComparison.Ordinal) ||
            normalized.EndsWith("id", StringComparison.Ordinal))
        {
            return false;
        }

        return SensitiveFieldNames.Contains(normalized) ||
               normalized.EndsWith("password", StringComparison.Ordinal) ||
               normalized.EndsWith("token", StringComparison.Ordinal) ||
               normalized.EndsWith("cpf", StringComparison.Ordinal) ||
               normalized.EndsWith("cnpj", StringComparison.Ordinal) ||
               normalized.EndsWith("email", StringComparison.Ordinal);
    }

    private static string Normalize(string propertyName)
    {
        return new string(propertyName.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    [GeneratedRegex("(?i)(\"(?:password|raw_token|token|cpf|cnpj|email)\"\\s*:\\s*\")(.*?)(\")")]
    private static partial Regex JsonSensitiveFieldRegex();

    [GeneratedRegex(@"(?i)((?:^|[?&]|\s+)(?:password|token|cpf|cnpj|email)=)([^&\s""]+)")]
    private static partial Regex QueryStringSensitiveFieldRegex();

    [GeneratedRegex(@"(?i)\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b")]
    private static partial Regex CpfRegex();

    [GeneratedRegex(@"\b\d{2}\.?\d{3}\.?\d{3}/?\d{4}-?\d{2}\b")]
    private static partial Regex CnpjRegex();
}
