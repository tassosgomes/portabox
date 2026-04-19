namespace PortaBox.Application.Abstractions.Identity;

public interface IIdentityUserProvisioningService
{
    Task<CreateSindicoUserResult> CreateSindicoUserAsync(
        CreateSindicoUserCommand command,
        CancellationToken cancellationToken = default);
}
