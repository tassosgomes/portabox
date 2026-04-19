using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortaBox.Application.Abstractions.Email;
using PortaBox.Application.Abstractions.Observability;
using PortaBox.Infrastructure.Observability;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Infrastructure.Email;

public sealed class EmailOutboxProcessor(
    AppDbContext dbContext,
    SmtpEmailDispatcher dispatcher,
    ILogger<EmailOutboxProcessor> logger,
    IGestaoMetrics metrics,
    TimeProvider timeProvider)
{
    private const int BatchSize = 20;

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var entries = await dbContext.Set<EmailOutboxEntry>()
            .Where(entry => entry.SentAt == null && entry.NextAttemptAt <= now)
            .OrderBy(entry => entry.NextAttemptAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var entry in entries)
        {
            var message = new EmailMessage(
                entry.ToAddress,
                entry.Subject,
                entry.HtmlBody,
                entry.TextBody);

            try
            {
                var attempts = await dispatcher.SendAsync(message, cancellationToken);
                entry.MarkAsSent(timeProvider.GetUtcNow());

                logger.LogInformation(
                    "Email outbox entry {outbox_id} sent successfully to_hash {to_hash} after {attempts} attempts.",
                    entry.Id,
                    EmailAddressHasher.Hash(entry.ToAddress),
                    attempts);
            }
            catch (Exception exception)
            {
                entry.RecordFailure(
                    EmailOutboxPolicy.RetryAttempts,
                    timeProvider.GetUtcNow().Add(EmailOutboxPolicy.ComputeNextDelay(entry.Attempts + EmailOutboxPolicy.RetryAttempts)),
                    exception.Message);

                logger.LogWarning(
                    exception,
                    "Email outbox entry {outbox_id} retry failed. to_hash {to_hash}, total_attempts {attempts}.",
                    entry.Id,
                    EmailAddressHasher.Hash(entry.ToAddress),
                    entry.Attempts);
            }
        }

        if (entries.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var oldestPendingAttempt = await dbContext.Set<EmailOutboxEntry>()
            .Where(entry => entry.SentAt == null)
            .OrderBy(entry => entry.NextAttemptAt)
            .Select(entry => entry.NextAttemptAt)
            .FirstOrDefaultAsync(cancellationToken);

        metrics.UpdateEmailOutboxAge(
            oldestPendingAttempt == default
                ? 0d
                : Math.Max(0d, (timeProvider.GetUtcNow() - oldestPendingAttempt).TotalSeconds));

        return entries.Count;
    }
}
