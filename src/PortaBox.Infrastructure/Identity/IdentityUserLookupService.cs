using Microsoft.EntityFrameworkCore;
using PortaBox.Application.Abstractions.Identity;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Infrastructure.Identity;

public sealed class IdentityUserLookupService(AppDbContext dbContext) : IIdentityUserLookupService
{
    public Task<IdentityUserLookup?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new IdentityUserLookup(
                user.Id,
                user.Email!,
                !string.IsNullOrWhiteSpace(user.PasswordHash),
                user.SindicoTenantId))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
