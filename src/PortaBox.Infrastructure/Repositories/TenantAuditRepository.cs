using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Infrastructure.Repositories;

public sealed class TenantAuditRepository(AppDbContext dbContext) : ITenantAuditRepository
{
    public Task AddAsync(TenantAuditEntry tenantAuditEntry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantAuditEntry);

        return dbContext.TenantAuditEntries.AddAsync(tenantAuditEntry, cancellationToken).AsTask();
    }
}
