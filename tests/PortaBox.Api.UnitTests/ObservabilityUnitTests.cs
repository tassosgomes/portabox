using System.Diagnostics.Metrics;
using System.Text.Json;
using PortaBox.Api.Infrastructure;
using PortaBox.Infrastructure.Observability;
using Serilog.Events;

namespace PortaBox.Api.UnitTests;

public sealed class ObservabilityUnitTests
{
    [Fact]
    public void SensitiveFieldSanitizer_ShouldMaskPasswordTokenAndPreserveNonSensitiveFields()
    {
        var sanitizer = new SensitiveFieldSanitizer();

        var sanitizedPassword = sanitizer.Sanitize("password", "hunter2");
        var sanitizedTokenInText = sanitizer.SanitizeText("payload with token=abc123 and email sindico@portabox.test");
        var preserved = sanitizer.Sanitize("subject", "Bem-vindo ao PortaBox");

        Assert.Equal("***", sanitizedPassword);
        Assert.DoesNotContain("abc123", sanitizedTokenInText, StringComparison.Ordinal);
        Assert.DoesNotContain("sindico@portabox.test", sanitizedTokenInText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Bem-vindo ao PortaBox", preserved);
    }

    [Fact]
    public void ApiJsonFormatter_ShouldSanitizeSensitiveProperties()
    {
        var formatter = new ApiJsonFormatter();
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            MessageTemplate.Empty,
            [
                new LogEventProperty("event", new ScalarValue("password-setup.failed")),
                new LogEventProperty("password", new ScalarValue("hunter2")),
                new LogEventProperty("token", new ScalarValue("abc123")),
                new LogEventProperty("email", new ScalarValue("sindico@portabox.test")),
                new LogEventProperty("template", new ScalarValue("MagicLinkPasswordSetup"))
            ]);

        using var writer = new StringWriter();
        formatter.Format(logEvent, writer);

        using var document = JsonDocument.Parse(writer.ToString());
        var root = document.RootElement;

        Assert.Equal("***", root.GetProperty("password").GetString());
        Assert.Equal("***", root.GetProperty("token").GetString());
        Assert.Equal("***", root.GetProperty("email").GetString());
        Assert.Equal("MagicLinkPasswordSetup", root.GetProperty("template").GetString());
    }

    [Fact]
    public void GestaoMetrics_IncrementCondominioCreated_ShouldEmitCounterMeasurement()
    {
        var measurements = new List<long>();
        using var listener = new MeterListener();

        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == PortaBoxDiagnostics.MeterName && instrument.Name == "condominio_created_total")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((_, measurement, _, _) => measurements.Add(measurement));
        listener.Start();

        var metrics = new GestaoMetrics();
        metrics.IncrementCondominioCreated();
        listener.RecordObservableInstruments();

        Assert.Contains(1L, measurements);
    }
}
