using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace PortaBox.Infrastructure.Observability;

public static class PortaBoxDiagnostics
{
    public const string ActivitySourceName = "PortaBox.Infrastructure";
    public const string MeterName = "PortaBox.Infrastructure";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);
}
