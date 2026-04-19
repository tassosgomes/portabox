namespace PortaBox.Application.Abstractions.Persistence;

public interface IApplicationDbSession
{
    Task<IApplicationDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
