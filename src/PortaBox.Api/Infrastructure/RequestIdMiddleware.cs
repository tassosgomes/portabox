using Serilog.Context;
using Microsoft.Extensions.Primitives;

namespace PortaBox.Api.Infrastructure;

public sealed class RequestIdMiddleware
{
    public const string HeaderName = "X-Request-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestIdMiddleware> _logger;

    public RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requestId = ResolveRequestId(context.Request.Headers[HeaderName]);

        context.TraceIdentifier = requestId;
        context.Response.Headers[HeaderName] = requestId;

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["request_id"] = requestId
        }))
        using (LogContext.PushProperty("request_id", requestId))
        {
            _logger.LogInformation("Request correlation established for {RequestPath}.", context.Request.Path);
            await _next(context);
        }
    }

    private static string ResolveRequestId(StringValues headerValues)
    {
        var requestId = headerValues.FirstOrDefault();

        return string.IsNullOrWhiteSpace(requestId)
            ? Guid.NewGuid().ToString("D")
            : requestId;
    }
}
