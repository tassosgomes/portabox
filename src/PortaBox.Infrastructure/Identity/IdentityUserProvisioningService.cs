using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using PortaBox.Application.Abstractions.Identity;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Infrastructure.Identity;

public sealed class IdentityUserProvisioningService(
    AppDbContext dbContext) : IIdentityUserProvisioningService
{
    public async Task<CreateSindicoUserResult> CreateSindicoUserAsync(
        CreateSindicoUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalizedEmail = command.Email.Trim();
        var normalizedEmailKey = normalizedEmail.ToUpperInvariant();
        var existingUser = await dbContext.Users
            .AnyAsync(user => user.NormalizedEmail == normalizedEmailKey, cancellationToken);

        if (existingUser)
        {
            return CreateSindicoUserResult.Failure("SindicoEmailAlreadyExists");
        }

        var sindicoRoleId = await dbContext.Roles
            .Where(role => role.NormalizedName == IdentityRoles.Sindico.ToUpperInvariant())
            .Select(role => role.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (sindicoRoleId == Guid.Empty)
        {
            return CreateSindicoUserResult.Failure("SindicoRoleNotFound");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            UserName = normalizedEmail,
            NormalizedEmail = normalizedEmailKey,
            NormalizedUserName = normalizedEmailKey,
            EmailConfirmed = true,
            LockoutEnabled = true,
            SindicoTenantId = command.TenantId,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };

        dbContext.Users.Add(user);
        dbContext.UserRoles.Add(new IdentityUserRole<Guid>
        {
            UserId = user.Id,
            RoleId = sindicoRoleId
        });

        return CreateSindicoUserResult.Success(new IdentityUserDescriptor(
            user.Id,
            user.Email!,
            command.FullName.Trim(),
            user.SindicoTenantId));
    }
}
