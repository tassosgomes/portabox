using PortaBox.Application.Abstractions;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Modules.Gestao.Application.Repositories;

namespace PortaBox.Modules.Gestao.Application.Queries.GetCondominioDetails;

public sealed class GetCondominioDetailsQueryHandler(
    ICondominioRepository condominioRepository,
    ITenantContext tenantContext) : IQueryHandler<GetCondominioDetailsQuery, CondominioDetailsDto>
{
    private const string NotFoundError = "gestao.condominio.not_found";

    public async Task<Result<CondominioDetailsDto>> HandleAsync(GetCondominioDetailsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var currentTenantId = tenantContext.TenantId;
        var details = await condominioRepository.GetDetailsAsync(
            query.CondominioId,
            ignoreTenantFilter: !currentTenantId.HasValue,
            tenantId: currentTenantId,
            cancellationToken);

        return details is null
            ? Result<CondominioDetailsDto>.Failure(NotFoundError)
            : Result<CondominioDetailsDto>.Success(details);
    }
}
