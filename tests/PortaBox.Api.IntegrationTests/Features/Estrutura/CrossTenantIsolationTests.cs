using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Api.IntegrationTests.Helpers;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Modules.Gestao.Application.Blocos;
using PortaBox.Modules.Gestao.Application.Unidades;

namespace PortaBox.Api.IntegrationTests.Features.Estrutura;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class CrossTenantIsolationTests(PostgresDatabaseFixture fixture)
{
    [Theory]
    [InlineData("GET", "/api/v1/condominios/{0}/estrutura")]
    [InlineData("POST", "/api/v1/condominios/{0}/blocos")]
    [InlineData("PATCH", "/api/v1/condominios/{0}/blocos/{1}")]
    [InlineData("POST", "/api/v1/condominios/{0}/blocos/{1}/unidades")]
    public async Task SindicoOfTenantA_ShouldNotAccessTenantBRoutes(string method, string routeTemplate)
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var tenantA = await EstruturaTestData.SeedActiveTenantAsync(factory.Services, tenantName: "Tenant A");
        var tenantB = await EstruturaTestData.SeedActiveTenantAsync(factory.Services, tenantName: "Tenant B");
        using var client = factory.CreateClient(TestAuthContext.SindicoOf(tenantA.CondominioId, tenantA.SindicoUserId));

        var route = string.Format(routeTemplate, tenantB.CondominioId, tenantB.BlocoId);
        using var request = new HttpRequestMessage(new HttpMethod(method), route);

        if (method == "POST" && route.Contains("/blocos/", StringComparison.Ordinal) && route.Contains("/unidades", StringComparison.Ordinal))
        {
            request.Content = JsonContent.Create(new { andar = 9, numero = "901" });
        }
        else if (method == "POST")
        {
            request.Content = JsonContent.Create(new { nome = "Bloco Invasor" });
        }
        else if (method == "PATCH")
        {
            request.Content = JsonContent.Create(new { nome = "Bloco Invadido" });
        }

        var response = await client.SendAsync(request);

        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
            $"Expected 403 or 404, but got {(int)response.StatusCode}.");
    }

    [Fact]
    public async Task TenantAScope_ShouldNotSeeTenantBEntities_InRepositories()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var tenantA = await EstruturaTestData.SeedActiveTenantAsync(factory.Services, tenantName: "Tenant A");
        var tenantB = await EstruturaTestData.SeedActiveTenantAsync(factory.Services, tenantName: "Tenant B");

        await using var scope = factory.Services.CreateAsyncScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        using var tenantScope = tenantContext.BeginScope(tenantA.CondominioId);

        var blocoRepository = scope.ServiceProvider.GetRequiredService<IBlocoRepository>();
        var unidadeRepository = scope.ServiceProvider.GetRequiredService<IUnidadeRepository>();

        var blocoB = await blocoRepository.GetByIdAsync(tenantB.BlocoId!.Value);
        var unidadeB = await unidadeRepository.GetByIdAsync(tenantB.Unidades[0].Id);
        var unidadesDoTenantA = await unidadeRepository.ListByCondominioAsync(tenantA.CondominioId, includeInactive: true, CancellationToken.None);

        Assert.Null(blocoB);
        Assert.Null(unidadeB);
        Assert.DoesNotContain(unidadesDoTenantA, current => current.Id == tenantB.Unidades[0].Id);
    }
}
