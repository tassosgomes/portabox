using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PortaBox.Application.Abstractions.Observability;
using PortaBox.Infrastructure.Observability;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Infrastructure.Events;

public sealed class DomainEventOutboxProcessor(
    AppDbContext dbContext,
    IOptions<DomainEventPublisherOptions> options,
    IGestaoMetrics metrics,
    ILogger<DomainEventOutboxProcessor> logger,
    TimeProvider timeProvider)
{
    public async Task<int> PublishPendingAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var batchSize = Math.Max(1, options.Value.BatchSize);

        var pendingIds = await dbContext.DomainEventOutboxEntries
            .Where(entry => entry.PublishedAt == null)
            .OrderBy(entry => entry.CreatedAt)
            .Select(entry => entry.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (pendingIds.Count > 0)
        {
            await dbContext.DomainEventOutboxEntries
                .Where(entry => pendingIds.Contains(entry.Id))
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(entry => entry.PublishedAt, now),
                    cancellationToken);
        }

        var pendingSnapshot = await dbContext.DomainEventOutboxEntries
            .Where(entry => entry.PublishedAt == null)
            .OrderBy(entry => entry.CreatedAt)
            .Select(entry => entry.CreatedAt)
            .ToListAsync(cancellationToken);

        var pendingCount = pendingSnapshot.Count;
        var oldestAgeSeconds = pendingCount == 0
            ? 0d
            : Math.Max(0d, (now - pendingSnapshot[0]).TotalSeconds);

        metrics.UpdateDomainEventOutboxPendingCount(pendingCount);

        logger.LogInformation(
            "Processed domain event outbox batch. {event} processed_count={processed_count} pending_count={pending_count} domain_event_outbox_age_seconds={domain_event_outbox_age_seconds}",
            "domain_event.outbox.processed",
            pendingIds.Count,
            pendingCount,
            oldestAgeSeconds);

        return pendingIds.Count;
    }
}
