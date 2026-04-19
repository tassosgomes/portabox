namespace PortaBox.Application.Abstractions.MultiTenancy;

/// <summary>
/// Provides the current tenant identifier for the active request or operation scope.
/// </summary>
/// <remarks>
/// Resolved as a scoped service per request. For HTTP requests the tenant is populated by
/// <c>TenantResolutionMiddleware</c> from the authenticated user's <c>tenant_id</c> claim (role
/// <c>Sindico</c>). For Operator flows the tenant is set explicitly via <see cref="BeginScope" />.
///
/// Rule: every entity that belongs to a tenant MUST implement <c>ITenantEntity</c> so that
/// the EF Core global query filter picks it up automatically and prevents cross-tenant data leakage.
/// </remarks>
public interface ITenantContext
{
    /// <summary>
    /// Gets the tenant identifier for the current scope, or <c>null</c> when no tenant has been
    /// established (e.g. public endpoints, background workers before a scope is opened).
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Opens a tenant scope that sets <see cref="TenantId" /> to <paramref name="tenantId" /> for the
    /// duration of the returned <see cref="IDisposable" />. When disposed, the previous value
    /// is restored.  Supports nesting.
    /// </summary>
    /// <param name="tenantId">The tenant to activate.</param>
    /// <returns>A disposable that restores the previous tenant on disposal.</returns>
    IDisposable BeginScope(Guid tenantId);
}
