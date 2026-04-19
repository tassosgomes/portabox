using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.Application.Repositories;

public interface IOptInDocumentRepository
{
    Task AddAsync(OptInDocument document, CancellationToken cancellationToken = default);

    Task<OptInDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
