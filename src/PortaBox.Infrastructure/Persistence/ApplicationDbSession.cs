using PortaBox.Application.Abstractions.Persistence;

namespace PortaBox.Infrastructure.Persistence;

public sealed class ApplicationDbSession(AppDbContext dbContext) : IApplicationDbSession
{
    public async Task<IApplicationDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new ApplicationDbTransaction(transaction);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
