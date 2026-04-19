using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PortaBox.Api.Middleware;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Infrastructure.MultiTenancy;

namespace PortaBox.Api.UnitTests;

public sealed class TenantContextTests
{
    // ─── TenantContext ────────────────────────────────────────────────────────

    [Fact]
    public void TenantId_ShouldBeNull_WhenNoScopeEstablished()
    {
        // Arrange
        var sut = new TenantContext();

        // Act + Assert
        Assert.Null(sut.TenantId);
    }

    [Fact]
    public void BeginScope_ShouldSetTenantId_ForDurationOfScope()
    {
        // Arrange
        var sut = new TenantContext();
        var tenantId = Guid.NewGuid();

        // Act + Assert — inside scope
        using (sut.BeginScope(tenantId))
        {
            Assert.Equal(tenantId, sut.TenantId);
        }

        // After dispose, TenantId must be restored to null
        Assert.Null(sut.TenantId);
    }

    [Fact]
    public void BeginScope_ShouldRestorePreviousTenantId_WhenNestedScopesAreUsed()
    {
        // Arrange
        var sut = new TenantContext();
        var outerTenant = Guid.NewGuid();
        var innerTenant = Guid.NewGuid();

        // Act + Assert
        using (sut.BeginScope(outerTenant))
        {
            Assert.Equal(outerTenant, sut.TenantId);

            using (sut.BeginScope(innerTenant))
            {
                Assert.Equal(innerTenant, sut.TenantId);
            }

            // Inner scope disposed → outer tenant restored
            Assert.Equal(outerTenant, sut.TenantId);
        }

        // Outer scope disposed → null restored
        Assert.Null(sut.TenantId);
    }

    [Fact]
    public void BeginScope_ShouldBeIdempotentOnMultipleDisposes()
    {
        // Arrange
        var sut = new TenantContext();
        var tenantId = Guid.NewGuid();

        var scope = sut.BeginScope(tenantId);
        scope.Dispose();
        scope.Dispose(); // second dispose must be a no-op

        Assert.Null(sut.TenantId);
    }

    [Fact]
    public async Task BeginScope_ShouldFlowTenantAcrossAsyncAwaitBoundaries()
    {
        // Arrange
        var sut = new TenantContext();
        var tenantId = Guid.NewGuid();

        // Act
        using (sut.BeginScope(tenantId))
        {
            await Task.Yield();

            // Assert
            Assert.Equal(tenantId, sut.TenantId);
        }

        Assert.Null(sut.TenantId);
    }

    // ─── TenantResolutionMiddleware ───────────────────────────────────────────

    [Fact]
    public async Task Middleware_ShouldSetTenantId_WhenUserIsSindicoWithTenantIdClaim()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenantContext = new TenantContext();

        var middleware = new TenantResolutionMiddleware(innerCtx =>
        {
            // Assert inside the pipeline — tenant must be populated here
            Assert.Equal(tenantId, tenantContext.TenantId);
            return Task.CompletedTask;
        });

        var httpContext = BuildHttpContext(
            role: TenantResolutionMiddleware.SindicoRole,
            tenantIdClaim: tenantId.ToString());

        // Act
        await middleware.InvokeAsync(httpContext, tenantContext);

        // After the pipeline, scope is disposed → null
        Assert.Null(tenantContext.TenantId);
    }

    [Fact]
    public async Task Middleware_ShouldNotSetTenantId_WhenUserIsOperator()
    {
        // Arrange
        var tenantContext = new TenantContext();

        var middleware = new TenantResolutionMiddleware(innerCtx =>
        {
            // Operator flows: TenantId should remain null
            Assert.Null(tenantContext.TenantId);
            return Task.CompletedTask;
        });

        var httpContext = BuildHttpContext(role: "Operator", tenantIdClaim: Guid.NewGuid().ToString());

        // Act
        await middleware.InvokeAsync(httpContext, tenantContext);

        Assert.Null(tenantContext.TenantId);
    }

    [Fact]
    public async Task Middleware_ShouldNotSetTenantId_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var tenantContext = new TenantContext();

        var middleware = new TenantResolutionMiddleware(innerCtx =>
        {
            Assert.Null(tenantContext.TenantId);
            return Task.CompletedTask;
        });

        var httpContext = new DefaultHttpContext(); // anonymous, no identity

        // Act
        await middleware.InvokeAsync(httpContext, tenantContext);

        Assert.Null(tenantContext.TenantId);
    }

    [Fact]
    public async Task Middleware_ShouldNotSetTenantId_WhenSindicoHasNoTenantIdClaim()
    {
        // Arrange
        var tenantContext = new TenantContext();

        var middleware = new TenantResolutionMiddleware(innerCtx =>
        {
            Assert.Null(tenantContext.TenantId);
            return Task.CompletedTask;
        });

        // Sindico role but no tenant_id claim
        var httpContext = BuildHttpContext(role: TenantResolutionMiddleware.SindicoRole, tenantIdClaim: null);

        // Act
        await middleware.InvokeAsync(httpContext, tenantContext);

        Assert.Null(tenantContext.TenantId);
    }

    [Fact]
    public async Task Middleware_ShouldNotSetTenantId_WhenTenantIdClaimIsInvalidGuid()
    {
        // Arrange
        var tenantContext = new TenantContext();

        var middleware = new TenantResolutionMiddleware(innerCtx =>
        {
            Assert.Null(tenantContext.TenantId);
            return Task.CompletedTask;
        });

        var httpContext = BuildHttpContext(
            role: TenantResolutionMiddleware.SindicoRole,
            tenantIdClaim: "not-a-guid");

        // Act
        await middleware.InvokeAsync(httpContext, tenantContext);

        Assert.Null(tenantContext.TenantId);
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static DefaultHttpContext BuildHttpContext(string role, string? tenantIdClaim)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };

        if (tenantIdClaim is not null)
        {
            claims.Add(new(TenantResolutionMiddleware.TenantIdClaimType, tenantIdClaim));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        var principal = new ClaimsPrincipal(identity);

        return new DefaultHttpContext { User = principal };
    }
}
