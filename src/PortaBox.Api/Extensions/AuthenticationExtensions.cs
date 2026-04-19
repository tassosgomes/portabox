using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using PortaBox.Infrastructure.Identity;

namespace PortaBox.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddIdentityBaseline(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        // Data Protection: persist keys so cookies survive application restarts.
        // In production, set DataProtection__KeysPath to a durable volume path.
        var keysPath = configuration["DataProtection:KeysPath"];
        var dpBuilder = services.AddDataProtection().SetApplicationName("PortaBox");
        if (!string.IsNullOrEmpty(keysPath))
        {
            dpBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }

        services
            .AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "portabox.auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = environment.IsDevelopment()
                ? SameSiteMode.Lax
                : SameSiteMode.Strict;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.SlidingExpiration = true;

            // Return HTTP status codes instead of browser redirects so API clients receive
            // 401 Unauthorized (not authenticated) and 403 Forbidden (authenticated but lacking role).
            options.Events.OnRedirectToLogin = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.RequireOperator, policy => policy.RequireRole(IdentityRoles.Operator));
            options.AddPolicy(AuthorizationPolicies.RequireSindico, policy => policy.RequireRole(IdentityRoles.Sindico));
        });

        return services;
    }
}

public static class AuthorizationPolicies
{
    public const string RequireOperator = nameof(RequireOperator);
    public const string RequireSindico = nameof(RequireSindico);
}
