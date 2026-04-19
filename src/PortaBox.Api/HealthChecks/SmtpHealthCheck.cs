using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PortaBox.Application.Abstractions.Email;

namespace PortaBox.Api.HealthChecks;

public sealed class SmtpHealthCheck(IOptions<EmailOptions> optionsAccessor) : IHealthCheck
{
    private readonly EmailOptions _options = optionsAccessor.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var tcpClient = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            await tcpClient.ConnectAsync(_options.Host, _options.Port, timeoutCts.Token);

            return tcpClient.Connected
                ? HealthCheckResult.Healthy("SMTP endpoint is reachable.")
                : HealthCheckResult.Unhealthy("SMTP endpoint is not reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("SMTP readiness probe failed.", exception);
        }
    }
}
