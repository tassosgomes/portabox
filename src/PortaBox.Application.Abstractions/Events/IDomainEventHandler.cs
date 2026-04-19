using PortaBox.Domain.Abstractions;

namespace PortaBox.Application.Abstractions.Events;

public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
