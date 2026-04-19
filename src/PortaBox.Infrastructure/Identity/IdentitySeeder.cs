using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

namespace PortaBox.Infrastructure.Identity;

public sealed class IdentitySeeder(
    RoleManager<AppRole> roleManager,
    UserManager<AppUser> userManager,
    IOptions<IdentityConfiguration> identityOptions,
    IHostEnvironment environment)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        foreach (var roleName in IdentityRoles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var createRoleResult = await roleManager.CreateAsync(new AppRole
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            });

            EnsureSucceeded(createRoleResult, $"Failed to create role '{roleName}'.");
        }

        if (!environment.IsDevelopment())
        {
            return;
        }

        var developmentOperator = identityOptions.Value.DevelopmentOperator;
        var userEmail = developmentOperator.Email.Trim();

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            throw new InvalidOperationException("Identity development operator email must be configured.");
        }

        if (string.IsNullOrWhiteSpace(developmentOperator.Password))
        {
            throw new InvalidOperationException("Identity development operator password must be configured.");
        }

        var user = await userManager.FindByEmailAsync(userEmail);
        if (user is null)
        {
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                Email = userEmail,
                UserName = userEmail,
                NormalizedEmail = userEmail.ToUpperInvariant(),
                NormalizedUserName = userEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                LockoutEnabled = true
            };

            var createUserResult = await userManager.CreateAsync(user, developmentOperator.Password);
            EnsureSucceeded(createUserResult, $"Failed to create development operator '{userEmail}'.");
        }

        if (!await userManager.IsInRoleAsync(user, IdentityRoles.Operator))
        {
            var addToRoleResult = await userManager.AddToRoleAsync(user, IdentityRoles.Operator);
            EnsureSucceeded(addToRoleResult, $"Failed to assign role '{IdentityRoles.Operator}' to '{userEmail}'.");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string errorMessage)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new InvalidOperationException($"{errorMessage} {errors}".Trim());
    }
}
