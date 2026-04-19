namespace PortaBox.Application.Abstractions.Persistence;

public interface IApplicationDbTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
