using Microsoft.EntityFrameworkCore.Storage;
using PortaBox.Application.Abstractions.Persistence;

namespace PortaBox.Infrastructure.Persistence;

public sealed class ApplicationDbTransaction(IDbContextTransaction transaction) : IApplicationDbTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return transaction.CommitAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return transaction.DisposeAsync();
    }
}
