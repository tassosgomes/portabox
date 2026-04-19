using PortaBox.Modules.Gestao.Application.Common;
using PortaBox.Modules.Gestao.Application.Queries.GetCondominioDetails;
using PortaBox.Modules.Gestao.Application.Queries.ListCondominios;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.Application.Repositories;

public interface ICondominioRepository
{
    Task AddAsync(Condominio condominio, CancellationToken cancellationToken = default);

    Task<Condominio?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCnpjAsync(string cnpj, CancellationToken cancellationToken = default);

    Task<CondominioDetailsDto?> GetDetailsAsync(
        Guid id,
        bool ignoreTenantFilter,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<CondominioListItemDto>> ListAsync(
        ListCondominiosQuery query,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Condominio condominio, CancellationToken cancellationToken = default);
}
