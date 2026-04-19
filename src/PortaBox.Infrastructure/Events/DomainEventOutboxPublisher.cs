using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PortaBox.Infrastructure.Events;

public sealed class DomainEventOutboxPublisher(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<DomainEventPublisherOptions> options,
    ILogger<DomainEventOutboxPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var publisherOptions = options.Value;

        if (!publisherOptions.Enabled)
        {
            logger.LogInformation("Domain event outbox publisher is disabled by configuration.");
            return;
        }

        var pollInterval = publisherOptions.PollInterval <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(15)
            : publisherOptions.PollInterval;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<DomainEventOutboxProcessor>();
                await processor.PublishPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unhandled exception while processing the domain event outbox publisher.");
            }

            await Task.Delay(pollInterval, stoppingToken);
        }
    }
}
