using PortaBox.Modules.Gestao.Domain.Blocos;

namespace PortaBox.Modules.Gestao.Application.Blocos;

public interface IBlocoRepository
{
    Task<Bloco?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Bloco?> GetByIdIncludingInactiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsActiveWithNameAsync(Guid condominioId, string nome, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Bloco>> ListByCondominioAsync(Guid condominioId, bool includeInactive, CancellationToken cancellationToken = default);

    Task AddAsync(Bloco bloco, CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);
}
