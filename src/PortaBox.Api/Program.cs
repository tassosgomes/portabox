using System.Diagnostics;
using PortaBox.Api.Features.Estrutura;
using PortaBox.Api.Endpoints;
using PortaBox.Api.Extensions;
using PortaBox.Api.Infrastructure;
using PortaBox.Api.Middleware;
using PortaBox.Api.Options;
using PortaBox.Infrastructure;
using PortaBox.Infrastructure.Identity;
using PortaBox.Modules.Gestao;
using Serilog;
using Serilog.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Host.UseSerilog((context, services, configuration) =>
{
    var enrichers = services.GetServices<ILogEventEnricher>().ToArray();

    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.With(enrichers)
        .WriteTo.Console(new ApiJsonFormatter());
});

builder.Services.AddPortaBoxApi(builder.Configuration, builder.Environment);
builder.Services.AddPortaBoxOpenTelemetry(builder.Configuration);
builder.Services.AddPortaBoxInfrastructure(builder.Configuration);
builder.Services.AddPortaBoxModuleGestao(builder.Configuration);
builder.Services.AddPortaBoxHealthChecks();

var app = builder.Build();

if (builder.Configuration.GetValue("Persistence:ApplyMigrationsOnStartup", true))
{
    await app.Services.ApplyIdentityMigrationsAndSeedAsync();
}

// Production configuration guards run at startup after the app is built.
ProductionConfigGuard.ValidateEmailTlsRequired(app.Configuration, app.Environment);
if (app.Configuration is IConfigurationRoot configRoot)
{
    ProductionConfigGuard.ValidateSecretsNotInJsonFiles(configRoot, app.Environment);
}

app.UseMiddleware<RequestIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    // Use SanitizedRequestPath (set by AccessLogSanitizerMiddleware) to prevent
    // raw token values from appearing in the access log.
    options.MessageTemplate =
        "HTTP {RequestMethod} {SanitizedRequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("request_id", httpContext.TraceIdentifier);

        if (Activity.Current is not null)
        {
            diagnosticContext.Set("trace_id", Activity.Current.TraceId.ToString());
            diagnosticContext.Set("span_id", Activity.Current.SpanId.ToString());
        }
    };
});
// Must run inside the Serilog scope so IDiagnosticContext is available for enrichment.
app.UseMiddleware<AccessLogSanitizerMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
    var problemDetailsService = statusCodeContext.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();

    await problemDetailsService.WriteAsync(new ProblemDetailsContext
    {
        HttpContext = statusCodeContext.HttpContext
    });
});
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors(ServiceCollectionExtensions.CorsPolicyName);
app.UseRateLimiter();
app.UseAuthentication();
// TenantResolutionMiddleware must run after UseAuthentication (principal is set) and before
// UseAuthorization so that downstream handlers can rely on TenantId being populated.
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Ok(new { service = "PortaBox.Api", status = "ok" }));
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy] = StatusCodes.Status200OK,
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded] = StatusCodes.Status200OK,
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    },
    ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync
});

var apiV1 = app.MapGroup(ApiRoutes.V1);

apiV1.MapGet("/", () => Results.Ok(new { service = "PortaBox.Api", version = "v1" }));
apiV1.MapAuthEndpoints();
apiV1.MapCondominiosEndpoints();
apiV1.MapEstruturaEndpoints();

if (builder.Configuration.GetValue<bool>("Testing:EnableExceptionEndpoint"))
{
    apiV1.MapGet("/_throw", (HttpContext _) => throw new InvalidOperationException("Test-only exception endpoint."));
}

app.Run();

public partial class Program;
