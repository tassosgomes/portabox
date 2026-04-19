using System.Text.RegularExpressions;
using Serilog;

namespace PortaBox.Api.Middleware;

public sealed partial class AccessLogSanitizerMiddleware(RequestDelegate next)
{
    public const string SanitizedPathProperty = "SanitizedRequestPath";

    [GeneratedRegex(@"(?<=[?&]token=)[^&\s#]+", RegexOptions.IgnoreCase)]
    private static partial Regex TokenValuePattern();

    public async Task InvokeAsync(HttpContext context, IDiagnosticContext diagnosticContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var query = SanitizeQueryString(context.Request.QueryString.Value ?? string.Empty);
        diagnosticContext.Set(SanitizedPathProperty, path + query);
        await next(context);
    }

    public static string SanitizeQueryString(string queryString)
    {
        if (string.IsNullOrEmpty(queryString)) return queryString;
        return TokenValuePattern().Replace(queryString, "[REDACTED]");
    }
}
