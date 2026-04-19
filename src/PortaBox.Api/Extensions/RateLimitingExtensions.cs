using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace PortaBox.Api.Extensions;

public static class RateLimitingExtensions
{
    public const string AuthPolicyName = "auth-fixed";

    public static IServiceCollection AddPortaBoxRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var maxRequests = configuration.GetValue("RateLimiting:Auth:MaxRequests", 10);
        var windowMinutes = configuration.GetValue("RateLimiting:Auth:WindowMinutes", 10);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(AuthPolicyName, context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = maxRequests,
                    Window = TimeSpan.FromMinutes(windowMinutes),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                await context.HttpContext.Response.WriteAsync(
                    "Too many requests. Please retry after the indicated period.", token);
            };
        });

        return services;
    }
}
