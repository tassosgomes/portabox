namespace PortaBox.Infrastructure.Events;

public sealed class DomainEventOutboxEntry
{
    private DomainEventOutboxEntry()
    {
    }

    public Guid Id { get; private set; }

    public Guid? TenantId { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public Guid AggregateId { get; private set; }

    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public static DomainEventOutboxEntry Create(
        Guid id,
        Guid? tenantId,
        string eventType,
        Guid aggregateId,
        string payload,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new DomainEventOutboxEntry
        {
            Id = id,
            TenantId = tenantId,
            EventType = eventType,
            AggregateId = aggregateId,
            Payload = payload,
            CreatedAt = createdAt
        };
    }
}
