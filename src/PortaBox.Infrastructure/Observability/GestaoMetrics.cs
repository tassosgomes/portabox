using System.Diagnostics.Metrics;
using PortaBox.Application.Abstractions.Observability;

namespace PortaBox.Infrastructure.Observability;

public sealed class GestaoMetrics : IGestaoMetrics
{
    private readonly Counter<long> _condominioCreatedCounter;
    private readonly Counter<long> _condominioActivatedCounter;
    private readonly Counter<long> _magicLinkIssuedCounter;
    private readonly Counter<long> _magicLinkConsumedCounter;
    private readonly Counter<long> _magicLinkExpiredCounter;
    private readonly Histogram<double> _emailSendDurationHistogram;
    private readonly object _sync = new();
    private long _domainEventOutboxPendingCount;
    private double _emailOutboxAgeSeconds;

    public GestaoMetrics()
    {
        _condominioCreatedCounter = PortaBoxDiagnostics.Meter.CreateCounter<long>("condominio_created_total");
        _condominioActivatedCounter = PortaBoxDiagnostics.Meter.CreateCounter<long>("condominio_activated_total");
        _magicLinkIssuedCounter = PortaBoxDiagnostics.Meter.CreateCounter<long>("magic_link_issued_total");
        _magicLinkConsumedCounter = PortaBoxDiagnostics.Meter.CreateCounter<long>("magic_link_consumed_total");
        _magicLinkExpiredCounter = PortaBoxDiagnostics.Meter.CreateCounter<long>("magic_link_expired_total");
        _emailSendDurationHistogram = PortaBoxDiagnostics.Meter.CreateHistogram<double>("email_send_duration_seconds", unit: "s");

        PortaBoxDiagnostics.Meter.CreateObservableGauge(
            "email_outbox_age_seconds",
            () => ReadEmailOutboxAgeSeconds());

        PortaBoxDiagnostics.Meter.CreateObservableGauge(
            "domain_event_outbox_pending_count",
            () => ReadDomainEventOutboxPendingCount());
    }

    public void IncrementCondominioCreated(string statusOutcome = "success")
    {
        _condominioCreatedCounter.Add(1, new KeyValuePair<string, object?>("status_outcome", statusOutcome));
    }

    public void IncrementCondominioActivated()
    {
        _condominioActivatedCounter.Add(1);
    }

    public void IncrementMagicLinkIssued(string purpose)
    {
        _magicLinkIssuedCounter.Add(1, new KeyValuePair<string, object?>("purpose", purpose));
    }

    public void IncrementMagicLinkConsumed(string purpose)
    {
        _magicLinkConsumedCounter.Add(1, new KeyValuePair<string, object?>("purpose", purpose));
    }

    public void IncrementMagicLinkExpired(string purpose)
    {
        _magicLinkExpiredCounter.Add(1, new KeyValuePair<string, object?>("purpose", purpose));
    }

    public void RecordEmailSendDuration(TimeSpan duration, string template, string outcome)
    {
        _emailSendDurationHistogram.Record(
            duration.TotalSeconds,
            new KeyValuePair<string, object?>("template", template),
            new KeyValuePair<string, object?>("outcome", outcome));
    }

    public void UpdateEmailOutboxAge(double oldestAgeSeconds)
    {
        lock (_sync)
        {
            _emailOutboxAgeSeconds = Math.Max(0d, oldestAgeSeconds);
        }
    }

    public void UpdateDomainEventOutboxPendingCount(long pendingCount)
    {
        lock (_sync)
        {
            _domainEventOutboxPendingCount = Math.Max(0L, pendingCount);
        }
    }

    private Measurement<double> ReadEmailOutboxAgeSeconds()
    {
        lock (_sync)
        {
            return new Measurement<double>(_emailOutboxAgeSeconds);
        }
    }

    private Measurement<long> ReadDomainEventOutboxPendingCount()
    {
        lock (_sync)
        {
            return new Measurement<long>(_domainEventOutboxPendingCount);
        }
    }
}
