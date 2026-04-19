using System.Security.Claims;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Infrastructure.MultiTenancy;

namespace PortaBox.Api.Middleware;

/// <summary>
/// ASP.NET Core middleware that resolves the current tenant from the authenticated principal and
/// populates <see cref="ITenantContext" />.
/// </summary>
/// <remarks>
/// Registration order (enforced in <c>Program.cs</c>):
/// <list type="number">
///   <item><c>UseAuthentication()</c> — must run first so the principal is populated.</item>
///   <item><c>UseMiddleware&lt;TenantResolutionMiddleware&gt;()</c> — this middleware.</item>
///   <item><c>UseAuthorization()</c> — runs after tenant is resolved.</item>
/// </list>
///
/// Resolution rules:
/// <list type="bullet">
///   <item>
///     Role <c>Sindico</c>: the <c>tenant_id</c> claim is read from the principal and used to call
///     <see cref="ITenantContext.BeginScope" />. The scope is held for the lifetime of the request.
///   </item>
///   <item>
///     Role <c>Operator</c>: tenant-target comes from the specific endpoint parameter (e.g. path /
///     query), NOT from a claim. This middleware does NOT set a scope for operators so that
///     endpoints can set it explicitly when needed.
///   </item>
///   <item>
///     Unauthenticated / anonymous requests: no scope is set; <c>TenantId</c> remains <c>null</c>.
///   </item>
/// </list>
/// </remarks>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    /// <summary>Name of the claim that carries the tenant identifier for Sindico users.</summary>
    public const string TenantIdClaimType = "tenant_id";

    /// <summary>Role name for condominium managers (síndicos).</summary>
    public const string SindicoRole = "Sindico";

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tenantContext);

        var user = context.User;

        if (user.Identity?.IsAuthenticated == true && user.IsInRole(SindicoRole))
        {
            var tenantIdValue = user.FindFirstValue(TenantIdClaimType);

            if (Guid.TryParse(tenantIdValue, out var tenantId))
            {
                // Keep the scope alive for the entire request.
                using (tenantContext.BeginScope(tenantId))
                {
                    await next(context);
                    return;
                }
            }
        }

        // Operator or anonymous: no scope set; proceed without a tenant filter.
        await next(context);
    }
}
