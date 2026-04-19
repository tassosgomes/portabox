using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PortaBox.Api.Middleware;
using Serilog.Core;
using Serilog.Events;

namespace PortaBox.Api.Infrastructure;

public sealed class HttpContextLogEnricher(IHttpContextAccessor httpContextAccessor) : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(httpContext.TraceIdentifier))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("request_id", httpContext.TraceIdentifier));
        }

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? httpContext.User.FindFirstValue("sub");
        if (!string.IsNullOrWhiteSpace(userId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("user_id", userId));
        }

        var tenantId = httpContext.User.FindFirstValue(TenantResolutionMiddleware.TenantIdClaimType);
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("tenant_id", tenantId));
        }
    }
}
