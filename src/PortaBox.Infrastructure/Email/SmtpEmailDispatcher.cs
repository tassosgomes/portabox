using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using PortaBox.Application.Abstractions.Email;

namespace PortaBox.Infrastructure.Email;

public sealed class SmtpEmailDispatcher
{
    private readonly IEmailTransport _transport;
    private readonly ILogger<SmtpEmailDispatcher> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public SmtpEmailDispatcher(
        IEmailTransport transport,
        ILogger<SmtpEmailDispatcher> logger)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                EmailOutboxPolicy.RetryAttempts - 1,
                attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)),
                (exception, delay, attempt, _) =>
                {
                    _logger.LogWarning(
                        exception,
                        "SMTP send attempt {Attempt} failed for email delivery. Retrying in {DelayMs} ms.",
                        attempt,
                        delay.TotalMilliseconds);
                });
    }

    public async Task<int> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var attemptCount = 0;

        await _retryPolicy.ExecuteAsync(async ct =>
        {
            attemptCount++;
            await _transport.SendAsync(message, ct);
        }, cancellationToken);

        return attemptCount;
    }
}
