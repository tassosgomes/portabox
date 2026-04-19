using PortaBox.Application.Abstractions;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Modules.Gestao.Application.Common;
using PortaBox.Modules.Gestao.Application.Repositories;

namespace PortaBox.Modules.Gestao.Application.Queries.ListCondominios;

public sealed class ListCondominiosQueryHandler(
    ICondominioRepository condominioRepository,
    ITenantContext tenantContext) : IQueryHandler<ListCondominiosQuery, PagedResult<CondominioListItemDto>>
{
    public async Task<Result<PagedResult<CondominioListItemDto>>> HandleAsync(ListCondominiosQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var result = await condominioRepository.ListAsync(query, tenantContext.TenantId, cancellationToken);
        return Result<PagedResult<CondominioListItemDto>>.Success(result);
    }
}
