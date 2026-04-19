using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PortaBox.Api.HealthChecks;
using PortaBox.Api.Infrastructure;
using PortaBox.Infrastructure.Observability;
using Serilog.Core;

namespace PortaBox.Api.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddPortaBoxOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var serviceName = configuration["OTEL_SERVICE_NAME"] ?? "portabox-api";
        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(PortaBoxDiagnostics.ActivitySourceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                Npgsql.TracerProviderBuilderExtensions.AddNpgsql(tracing);

                if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = endpoint);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(PortaBoxDiagnostics.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
                {
                    metrics.AddOtlpExporter(options => options.Endpoint = endpoint);
                }
            });

        return services;
    }

    public static IServiceCollection AddPortaBoxHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Process is alive."), tags: ["live"])
            .AddCheck<DatabaseHealthCheck>("db", failureStatus: HealthStatus.Unhealthy, tags: ["ready"])
            .AddCheck<StorageHealthCheck>("storage", failureStatus: HealthStatus.Unhealthy, tags: ["ready"])
            .AddCheck<SmtpHealthCheck>("smtp", failureStatus: HealthStatus.Unhealthy, tags: ["ready"]);

        return services;
    }

    public static IServiceCollection AddPortaBoxLogEnrichers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<SensitiveFieldSanitizer>();
        services.AddSingleton<ILogEventEnricher, ActivityEnricher>();
        services.AddSingleton<ILogEventEnricher, HttpContextLogEnricher>();

        return services;
    }
}
