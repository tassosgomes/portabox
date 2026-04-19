namespace PortaBox.Domain.Abstractions;

public interface IDomainEvent
{
    string EventType { get; }

    DateTimeOffset OccurredAt { get; }
}
