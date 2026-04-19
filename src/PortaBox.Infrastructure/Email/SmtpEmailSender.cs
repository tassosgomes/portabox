using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortaBox.Application.Abstractions.Email;
using PortaBox.Application.Abstractions.Observability;
using PortaBox.Infrastructure.Observability;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Infrastructure.Email;

public sealed class SmtpEmailSender(
    SmtpEmailDispatcher dispatcher,
    AppDbContext dbContext,
    ILogger<SmtpEmailSender> logger,
    IGestaoMetrics metrics,
    TimeProvider timeProvider) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var toHash = EmailAddressHasher.Hash(message.To);
        var template = message.Tags is not null &&
                       message.Tags.TryGetValue("template", out var templateName) &&
                       !string.IsNullOrWhiteSpace(templateName)
            ? templateName
            : "unknown";
        var startedAt = timeProvider.GetTimestamp();

        using var activity = PortaBoxDiagnostics.ActivitySource.StartActivity("email.send");
        activity?.SetTag("email.template", template);
        activity?.SetTag("email.to_hash", toHash);

        try
        {
            var attempts = await dispatcher.SendAsync(message, cancellationToken);
            metrics.RecordEmailSendDuration(timeProvider.GetElapsedTime(startedAt), template, "success");

            logger.LogInformation(
                "Transactional email sent successfully. {event} subject={subject} template={template} to_hash={to_hash} attempts={attempts}",
                "email.sent",
                message.Subject,
                template,
                toHash,
                attempts);
        }
        catch (Exception exception)
        {
            metrics.RecordEmailSendDuration(timeProvider.GetElapsedTime(startedAt), template, "failure");
            var attempts = EmailOutboxPolicy.RetryAttempts;
            var now = timeProvider.GetUtcNow();
            var entry = EmailOutboxEntry.Create(
                message.To,
                message.Subject,
                message.HtmlBody,
                message.TextBody,
                attempts,
                now.Add(EmailOutboxPolicy.ComputeNextDelay(attempts)),
                exception.Message);

            dbContext.Set<EmailOutboxEntry>().Add(entry);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogError(
                exception,
                "Transactional email delivery failed. {event} outbox_id={outbox_id} subject={subject} template={template} to_hash={to_hash} attempts={attempts} error_kind={error_kind}",
                "email.failed",
                entry.Id,
                entry.Subject,
                template,
                toHash,
                entry.Attempts,
                exception.GetType().Name);
        }
    }
}
