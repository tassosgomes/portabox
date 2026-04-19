using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.Application.Repositories;

public interface ITenantAuditRepository
{
    Task AddAsync(TenantAuditEntry tenantAuditEntry, CancellationToken cancellationToken = default);
}
