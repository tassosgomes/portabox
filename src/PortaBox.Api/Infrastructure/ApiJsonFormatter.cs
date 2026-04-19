using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;
using PortaBox.Infrastructure.Observability;

namespace PortaBox.Api.Infrastructure;

public sealed class ApiJsonFormatter : ITextFormatter
{
    private static readonly SensitiveFieldSanitizer Sanitizer = new();

    private static readonly HashSet<string> ReservedProperties =
    [
        "request_id",
        "trace_id",
        "span_id",
        "user_id",
        "tenant_id",
        "event"
    ];

    public void Format(LogEvent logEvent, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(output);

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["timestamp"] = logEvent.Timestamp.UtcDateTime,
            ["level"] = logEvent.Level.ToString(),
            ["message"] = Sanitizer.SanitizeText(logEvent.RenderMessage()),
            ["request_id"] = GetScalarString(logEvent, "request_id"),
            ["trace_id"] = GetScalarString(logEvent, "trace_id"),
            ["span_id"] = GetScalarString(logEvent, "span_id"),
            ["user_id"] = GetScalarString(logEvent, "user_id"),
            ["tenant_id"] = GetScalarString(logEvent, "tenant_id"),
            ["event"] = GetScalarString(logEvent, "event")
        };

        if (logEvent.Exception is not null)
        {
            payload["exception"] = Sanitizer.SanitizeText(logEvent.Exception.ToString());
        }

        foreach (var property in logEvent.Properties)
        {
            if (ReservedProperties.Contains(property.Key))
            {
                continue;
            }

            payload[property.Key] = Sanitizer.Sanitize(property.Key, SerializePropertyValue(property.Value));
        }

        output.Write(JsonSerializer.Serialize(payload));
        output.WriteLine();
    }

    private static string? GetScalarString(LogEvent logEvent, string propertyName)
    {
        if (!logEvent.Properties.TryGetValue(propertyName, out var value))
        {
            return null;
        }

        return SerializePropertyValue(value)?.ToString();
    }

    private static object? SerializePropertyValue(LogEventPropertyValue propertyValue)
    {
        return propertyValue switch
        {
            ScalarValue scalar => scalar.Value,
            SequenceValue sequence => sequence.Elements.Select(SerializePropertyValue).ToArray(),
            StructureValue structure => structure.Properties.ToDictionary(
                property => property.Name,
                property => SerializePropertyValue(property.Value),
                StringComparer.Ordinal),
            DictionaryValue dictionary => dictionary.Elements.ToDictionary(
                element => element.Key.Value?.ToString() ?? string.Empty,
                element => SerializePropertyValue(element.Value),
                StringComparer.Ordinal),
            _ => propertyValue.ToString()
        };
    }
}
