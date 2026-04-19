using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.Application.Repositories;

public interface ISindicoRepository
{
    Task AddAsync(Sindico sindico, CancellationToken cancellationToken = default);

    Task<Sindico?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Sindico?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Sindico?> GetByUserIdIgnoreQueryFiltersAsync(Guid userId, CancellationToken cancellationToken = default);

    Task UpdateAsync(Sindico sindico, CancellationToken cancellationToken = default);
}
