using Microsoft.EntityFrameworkCore;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Infrastructure.Repositories;

public sealed class OptInDocumentRepository(AppDbContext dbContext) : IOptInDocumentRepository
{
    public Task AddAsync(OptInDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        return dbContext.OptInDocuments.AddAsync(document, cancellationToken).AsTask();
    }

    public Task<OptInDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.OptInDocuments.FirstOrDefaultAsync(document => document.Id == id, cancellationToken);
    }
}
