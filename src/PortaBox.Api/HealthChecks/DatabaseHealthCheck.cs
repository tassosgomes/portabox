using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace PortaBox.Api.HealthChecks;

public sealed class DatabaseHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.CreateCommand("SELECT 1");
        var result = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToInt32(result) == 1
            ? HealthCheckResult.Healthy("Database responded to SELECT 1.")
            : HealthCheckResult.Unhealthy("Database readiness probe returned an unexpected result.");
    }
}
