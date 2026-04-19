using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.Application.Repositories;

public interface IOptInRecordRepository
{
    Task AddAsync(OptInRecord optInRecord, CancellationToken cancellationToken = default);

    Task<OptInRecord?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
