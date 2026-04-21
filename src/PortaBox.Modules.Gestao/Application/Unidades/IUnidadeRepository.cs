using PortaBox.Modules.Gestao.Domain.Unidades;

namespace PortaBox.Modules.Gestao.Application.Unidades;

public interface IUnidadeRepository
{
    Task<Unidade?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Unidade?> GetByIdIncludingInactiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Unidade?> FindActiveByCanonicalAsync(
        Guid tenantId,
        Guid blocoId,
        int andar,
        string numero,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsActiveWithCanonicalAsync(
        Guid tenantId,
        Guid blocoId,
        int andar,
        string numero,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Unidade>> ListByBlocoAsync(Guid blocoId, bool includeInactive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Unidade>> ListByCondominioAsync(Guid condominioId, bool includeInactive, CancellationToken cancellationToken = default);

    Task AddAsync(Unidade unidade, CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);
}
