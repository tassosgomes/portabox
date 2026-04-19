using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using PortaBox.Application.Abstractions.Events;
using PortaBox.Domain.Abstractions;
using PortaBox.Infrastructure.Events;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Api.UnitTests;

public sealed class DomainEventInfrastructureTests
{
    [Fact]
    public async Task DispatchAsync_ShouldResolveHandlersAndInvokeThemInRegistrationOrder()
    {
        var calls = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventHandler<TestDomainEvent>>(new RecordingHandler("first", calls));
        services.AddSingleton<IDomainEventHandler<TestDomainEvent>>(new RecordingHandler("second", calls));

        var provider = services.BuildServiceProvider();
        var dispatcher = new InProcessDomainEventDispatcher(provider);

        await dispatcher.DispatchAsync([new TestDomainEvent("condominio.cadastrado.v1", "Residencial Aurora")]);

        Assert.Equal(["first:Residencial Aurora", "second:Residencial Aurora"], calls);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldSerializeEventsIntoOutboxAndDispatchAfterCommit()
    {
        var recordedBatches = new List<IReadOnlyList<IDomainEvent>>();
        var dispatcher = new RecordingDispatcher(recordedBatches);
        var interceptor = new DomainEventOutboxInterceptor(dispatcher, new FixedTimeProvider(new DateTimeOffset(2026, 4, 18, 12, 0, 0, TimeSpan.Zero)));

        await using var dbContext = BuildDbContext(interceptor);
        var condominio = Condominio.Create(
            Guid.NewGuid(),
            "Residencial Aurora",
            "12.345.678/0001-95",
            Guid.NewGuid(),
            TimeProvider.System);
        var domainEvent = new TestDomainEvent("condominio.cadastrado.v1", condominio.NomeFantasia);

        AddDomainEvent(condominio, domainEvent);
        dbContext.Condominios.Add(condominio);

        await dbContext.SaveChangesAsync();

        var outboxEntry = await dbContext.DomainEventOutboxEntries.SingleAsync();
        using var payload = JsonDocument.Parse(outboxEntry.Payload);

        Assert.Equal(domainEvent.EventType, outboxEntry.EventType);
        Assert.Equal(condominio.Id, outboxEntry.AggregateId);
        Assert.Equal(condominio.Id, outboxEntry.TenantId);
        Assert.Equal("Residencial Aurora", payload.RootElement.GetProperty("aggregateName").GetString());
        Assert.Equal(domainEvent.EventType, payload.RootElement.GetProperty("eventType").GetString());
        Assert.Empty(condominio.DomainEvents);
        Assert.Single(recordedBatches);
        Assert.Single(recordedBatches[0]);
        Assert.Same(domainEvent, recordedBatches[0][0]);
    }

    private static void AddDomainEvent(AggregateRoot aggregateRoot, IDomainEvent domainEvent)
    {
        typeof(AggregateRoot)
            .GetMethod("AddDomainEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(aggregateRoot, [domainEvent]);
    }

    private static AppDbContext BuildDbContext(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .AddInterceptors(interceptors)
            .Options;

        return new AppDbContext(options);
    }

    private sealed class RecordingDispatcher(List<IReadOnlyList<IDomainEvent>> recordedBatches) : IDomainEventDispatcher
    {
        public Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            recordedBatches.Add(domainEvents.ToList());
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingHandler(string name, List<string> calls) : IDomainEventHandler<TestDomainEvent>
    {
        public Task HandleAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            calls.Add($"{name}:{domainEvent.AggregateName}");
            return Task.CompletedTask;
        }
    }

    private sealed record TestDomainEvent(string EventType, string AggregateName) : IDomainEvent
    {
        public DateTimeOffset OccurredAt { get; } = new(2026, 4, 18, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
