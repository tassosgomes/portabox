namespace PortaBox.Application.Abstractions.Identity;

public interface IIdentityPasswordService
{
    Task<SetPasswordResult> AddPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default);
}
