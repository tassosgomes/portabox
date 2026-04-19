namespace PortaBox.Application.Abstractions.Observability;

public interface IGestaoMetrics
{
    void IncrementCondominioCreated(string statusOutcome = "success");

    void IncrementCondominioActivated();

    void IncrementMagicLinkIssued(string purpose);

    void IncrementMagicLinkConsumed(string purpose);

    void IncrementMagicLinkExpired(string purpose);

    void RecordEmailSendDuration(TimeSpan duration, string template, string outcome);

    void UpdateEmailOutboxAge(double oldestAgeSeconds);

    void UpdateDomainEventOutboxPendingCount(long pendingCount);
}
