using System.Reflection;
using PortaBox.Domain.Abstractions;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class AggregateRootTests
{
    [Fact]
    public void AddDomainEvent_ShouldAccumulateEventInDomainEventsCollection()
    {
        var aggregate = new TestAggregate();
        var firstEvent = new TestDomainEvent("condominio.cadastrado.v1");
        var secondEvent = new TestDomainEvent("condominio.ativado.v1");

        aggregate.Raise(firstEvent);
        aggregate.Raise(secondEvent);

        Assert.Collection(
            aggregate.DomainEvents,
            domainEvent => Assert.Same(firstEvent, domainEvent),
            domainEvent => Assert.Same(secondEvent, domainEvent));
    }

    [Fact]
    public void ClearDomainEvents_ShouldBeInternalOnly()
    {
        var method = typeof(AggregateRoot).GetMethod(
            "ClearDomainEvents",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.NotNull(method);
        Assert.True(method!.IsAssembly);
        Assert.False(method.IsPublic);
        Assert.False(method.IsFamily);
    }

    private sealed class TestAggregate : AggregateRoot
    {
        public void Raise(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    private sealed record TestDomainEvent(string EventType) : IDomainEvent
    {
        public DateTimeOffset OccurredAt { get; } = new(2026, 4, 18, 12, 0, 0, TimeSpan.Zero);
    }
}
