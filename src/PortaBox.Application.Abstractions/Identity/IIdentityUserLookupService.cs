namespace PortaBox.Application.Abstractions.Identity;

public interface IIdentityUserLookupService
{
    Task<IdentityUserLookup?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
