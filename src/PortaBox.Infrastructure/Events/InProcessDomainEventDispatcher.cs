using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PortaBox.Application.Abstractions.Events;
using PortaBox.Domain.Abstractions;

namespace PortaBox.Infrastructure.Events;

public sealed class InProcessDomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly MethodInfo DispatchSingleMethod = typeof(InProcessDomainEventDispatcher)
        .GetMethod(nameof(DispatchSingleAsync), BindingFlags.Instance | BindingFlags.NonPublic)!;

    public async Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            var dispatchTask = (Task)DispatchSingleMethod
                .MakeGenericMethod(domainEvent.GetType())
                .Invoke(this, [domainEvent, cancellationToken])!;

            await dispatchTask;
        }
    }

    private async Task DispatchSingleAsync<TDomainEvent>(TDomainEvent domainEvent, CancellationToken cancellationToken)
        where TDomainEvent : IDomainEvent
    {
        var handlers = serviceProvider.GetServices<IDomainEventHandler<TDomainEvent>>();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(domainEvent, cancellationToken);
        }
    }
}
