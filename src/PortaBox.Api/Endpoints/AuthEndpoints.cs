using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using PortaBox.Api.Extensions;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Infrastructure.Identity;
using PortaBox.Modules.Gestao.Application.Commands.PasswordSetup;

namespace PortaBox.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<AppUser> userManager,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            {
                return Results.Unauthorized();
            }

            var roles = await userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            if (roles.Contains(IdentityRoles.Sindico) && user.SindicoTenantId.HasValue)
            {
                claims.Add(new Claim(Middleware.TenantResolutionMiddleware.TenantIdClaimType, user.SindicoTenantId.Value.ToString()));
            }

            var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal);

            var primaryRole = roles.Contains(IdentityRoles.Operator) ? IdentityRoles.Operator
                : roles.Contains(IdentityRoles.Sindico) ? IdentityRoles.Sindico
                : null;

            return Results.Ok(new LoginResponse(user.Id, primaryRole, user.SindicoTenantId));
        }).AllowAnonymous().RequireRateLimiting(RateLimitingExtensions.AuthPolicyName);

        group.MapPost("/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapPost("/password-setup", async (
            PasswordSetupRequest request,
            ICommandHandler<PasswordSetupCommand, PasswordSetupResult> handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var result = await handler.HandleAsync(
                new PasswordSetupCommand(request.Token, request.Password, clientIp),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok()
                : Results.Problem(
                    title: "Invalid or expired token.",
                    statusCode: StatusCodes.Status400BadRequest);
        }).AllowAnonymous().RequireRateLimiting(RateLimitingExtensions.AuthPolicyName);

        group.MapGet("/me", (HttpContext httpContext) =>
        {
            var user = httpContext.User;
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = user.FindFirstValue(ClaimTypes.Email);
            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
            var tenantIdValue = user.FindFirstValue(Middleware.TenantResolutionMiddleware.TenantIdClaimType);
            Guid? tenantId = Guid.TryParse(tenantIdValue, out var tid) ? tid : null;

            return Results.Ok(new MeResponse(Guid.Parse(userId!), email!, roles, tenantId));
        }).RequireAuthorization();

        return routes;
    }

    public sealed record LoginRequest(string Email, string Password);

    public sealed record LoginResponse(Guid UserId, string? Role, Guid? TenantId);

    public sealed record PasswordSetupRequest(string Token, string Password);

    public sealed record MeResponse(Guid UserId, string Email, string[] Roles, Guid? TenantId);
}
