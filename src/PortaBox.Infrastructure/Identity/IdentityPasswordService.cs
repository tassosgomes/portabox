using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortaBox.Application.Abstractions.Identity;

namespace PortaBox.Infrastructure.Identity;

public sealed class IdentityPasswordService(UserManager<AppUser> userManager) : IIdentityPasswordService
{
    public async Task<SetPasswordResult> AddPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.SingleOrDefaultAsync(current => current.Id == userId, cancellationToken);
        if (user is null)
        {
            return SetPasswordResult.Failure("user_not_found");
        }

        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return SetPasswordResult.Failure("password_already_set");
        }

        var result = await userManager.AddPasswordAsync(user, password);
        if (result.Succeeded)
        {
            return SetPasswordResult.Success();
        }

        var firstErrorCode = result.Errors.Select(error => error.Code).FirstOrDefault();
        var normalizedErrorCode = firstErrorCode is not null && firstErrorCode.StartsWith("Password", StringComparison.Ordinal)
            ? "password_policy"
            : "identity_failure";

        return SetPasswordResult.Failure(normalizedErrorCode);
    }
}
