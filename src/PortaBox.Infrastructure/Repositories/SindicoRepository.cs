using Microsoft.EntityFrameworkCore;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Infrastructure.Repositories;

public sealed class SindicoRepository(AppDbContext dbContext) : ISindicoRepository
{
    public Task AddAsync(Sindico sindico, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sindico);

        return dbContext.Sindicos.AddAsync(sindico, cancellationToken).AsTask();
    }

    public Task<Sindico?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Sindicos.FirstOrDefaultAsync(sindico => sindico.Id == id, cancellationToken);
    }

    public Task<Sindico?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.Sindicos.FirstOrDefaultAsync(sindico => sindico.UserId == userId, cancellationToken);
    }

    public Task<Sindico?> GetByUserIdIgnoreQueryFiltersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.Sindicos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(sindico => sindico.UserId == userId, cancellationToken);
    }

    public Task UpdateAsync(Sindico sindico, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sindico);

        dbContext.Sindicos.Update(sindico);
        return Task.CompletedTask;
    }
}
