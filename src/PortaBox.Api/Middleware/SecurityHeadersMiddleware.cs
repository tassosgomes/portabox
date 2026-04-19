namespace PortaBox.Api.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private static readonly string[] SwaggerInlineScriptHashes =
    [
        "'sha256-Tui7QoFlnLXkJCSl1/JvEZdIXTmBttnWNxzJpXomQjg='",
        "'sha256-zk2KIqMc+Rb3uIRoYw2kLlVGkF0IOVkm0nb7+pVYEI0='"
    ];

    public const string CspValue =
        "default-src 'self'; " +
        "img-src 'self' data: https://fonts.gstatic.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'";

    public static readonly string SwaggerCspValue =
        $"{CspValue}; script-src 'self' {string.Join(" ", SwaggerInlineScriptHashes)}";

    public async Task InvokeAsync(HttpContext context)
    {
        var h = context.Response.Headers;
        h["Content-Security-Policy"] = IsSwaggerRequest(context.Request.Path)
            ? SwaggerCspValue
            : CspValue;
        h["X-Content-Type-Options"] = "nosniff";
        h["X-Frame-Options"] = "DENY";
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";
        await next(context);
    }

    private static bool IsSwaggerRequest(PathString path)
    {
        return path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase);
    }
}
