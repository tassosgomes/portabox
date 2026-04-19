using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PortaBox.Application.Abstractions.Events;
using PortaBox.Domain.Abstractions;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Infrastructure.Events;

public sealed class DomainEventOutboxInterceptor(
    IDomainEventDispatcher domainEventDispatcher,
    TimeProvider timeProvider) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly ConditionalWeakTable<DbContext, PendingDispatchState> _pendingDispatchStates = new();

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        PrepareOutboxEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        PrepareOutboxEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchAndClear(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchAndClear(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        RestorePendingDomainEvents(eventData.Context);
        base.SaveChangesFailed(eventData);
    }

    public override async Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        RestorePendingDomainEvents(eventData.Context);
        await base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private async Task DispatchAndClear(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null || !_pendingDispatchStates.TryGetValue(context, out var pendingState))
        {
            return;
        }

        _pendingDispatchStates.Remove(context);
        await domainEventDispatcher.DispatchAsync(pendingState.DomainEvents, cancellationToken);
    }

    private void PrepareOutboxEntries(DbContext? context)
    {
        if (context is not AppDbContext appDbContext)
        {
            return;
        }

        var aggregatesWithEvents = appDbContext.ChangeTracker
            .Entries()
            .Where(entry => entry.Entity is AggregateRoot aggregateRoot && aggregateRoot.DomainEvents.Count > 0)
            .Select(entry => BuildAggregateBatch(entry, (AggregateRoot)entry.Entity))
            .ToList();

        if (aggregatesWithEvents.Count == 0)
        {
            return;
        }

        var createdAt = timeProvider.GetUtcNow();
        var outboxEntries = aggregatesWithEvents
            .SelectMany(aggregate => aggregate.DomainEvents.Select(domainEvent => DomainEventOutboxEntry.Create(
                Guid.NewGuid(),
                aggregate.TenantId,
                domainEvent.EventType,
                aggregate.AggregateId,
                JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonSerializerOptions),
                createdAt)))
            .ToList();

        appDbContext.DomainEventOutboxEntries.AddRange(outboxEntries);

        foreach (var aggregate in aggregatesWithEvents)
        {
            aggregate.AggregateRoot.ClearDomainEvents();
        }

        _pendingDispatchStates.Remove(appDbContext);
        _pendingDispatchStates.Add(appDbContext, new PendingDispatchState(aggregatesWithEvents, outboxEntries));
    }

    private static AggregateBatch BuildAggregateBatch(EntityEntry entry, AggregateRoot aggregateRoot)
    {
        var aggregateId = ResolveAggregateId(entry);
        var tenantId = ResolveTenantId(entry.Entity, aggregateId);

        return new AggregateBatch(
            aggregateRoot,
            aggregateRoot.DomainEvents.ToList(),
            aggregateId,
            tenantId);
    }

    private static Guid ResolveAggregateId(EntityEntry entry)
    {
        var idProperty = entry.Properties.SingleOrDefault(property =>
            string.Equals(property.Metadata.Name, nameof(Condominio.Id), StringComparison.Ordinal));

        if (idProperty?.CurrentValue is Guid aggregateId)
        {
            return aggregateId;
        }

        throw new InvalidOperationException($"Aggregate '{entry.Entity.GetType().Name}' must expose a Guid Id property to use domain events.");
    }

    private static Guid? ResolveTenantId(object entity, Guid aggregateId)
    {
        if (entity is ITenantEntity tenantEntity)
        {
            return tenantEntity.TenantId;
        }

        return entity is Condominio ? aggregateId : null;
    }

    private void RestorePendingDomainEvents(DbContext? context)
    {
        if (context is null || !_pendingDispatchStates.TryGetValue(context, out var pendingState))
        {
            return;
        }

        foreach (var aggregate in pendingState.Aggregates)
        {
            aggregate.AggregateRoot.RestoreDomainEvents(aggregate.DomainEvents);
        }

        foreach (var outboxEntry in pendingState.OutboxEntries)
        {
            context.Entry(outboxEntry).State = EntityState.Detached;
        }

        _pendingDispatchStates.Remove(context);
    }

    private sealed record AggregateBatch(
        AggregateRoot AggregateRoot,
        IReadOnlyList<IDomainEvent> DomainEvents,
        Guid AggregateId,
        Guid? TenantId);

    private sealed record PendingDispatchState(
        IReadOnlyList<AggregateBatch> Aggregates,
        IReadOnlyList<DomainEventOutboxEntry> OutboxEntries)
    {
        public IReadOnlyList<IDomainEvent> DomainEvents => Aggregates
            .SelectMany(aggregate => aggregate.DomainEvents)
            .ToList();
    }
}
