namespace PortaBox.Domain.Abstractions;

/// <summary>
/// Marker interface that every multi-tenant domain entity MUST implement.
/// </summary>
/// <remarks>
/// The presence of this interface is the signal used by <c>AppDbContext.OnModelCreating</c> to apply
/// the EF Core global query filter <c>e.TenantId == _tenantContext.TenantId</c> automatically for
/// all types that implement it.
///
/// Convention: column name is always <c>tenant_id</c> (snake_case via EF naming convention).
/// Entities that are intentionally global (e.g. <c>Condominio</c> itself, <c>AspNetUsers</c>,
/// audit tables) MUST NOT implement this interface.
/// </remarks>
public interface ITenantEntity
{
    /// <summary>Gets the tenant this entity belongs to.</summary>
    Guid TenantId { get; }
}
