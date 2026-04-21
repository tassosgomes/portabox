using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Api.IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace PortaBox.Api.IntegrationTests.Features.Estrutura;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class EstruturaEndpointsTests(PostgresDatabaseFixture fixture, ITestOutputHelper output)
{
    [Fact]
    public async Task GetEstrutura_SindicoOwnTenant_ShouldReturnTree_AndOtherTenantShouldBeRejected()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var tenantA = await EstruturaTestData.SeedActiveTenantAsync(factory.Services, tenantName: "Residencial Alfa");
        var tenantB = await EstruturaTestData.SeedActiveTenantAsync(factory.Services, tenantName: "Residencial Beta");

        using var client = factory.CreateClient(TestAuthContext.SindicoOf(tenantA.CondominioId, tenantA.SindicoUserId));

        var ownResponse = await client.GetAsync($"/api/v1/condominios/{tenantA.CondominioId}/estrutura");
        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);

        using (var ownJson = await EstruturaTestData.ReadJsonAsync(ownResponse))
        {
            Assert.Equal(tenantA.CondominioId, ownJson.RootElement.GetProperty("condominioId").GetGuid());
            Assert.Equal("Residencial Alfa", ownJson.RootElement.GetProperty("nomeFantasia").GetString());
            var blocos = ownJson.RootElement.GetProperty("blocos");
            Assert.Single(blocos.EnumerateArray());
            Assert.Equal(3, blocos[0].GetProperty("andares").EnumerateArray().Sum(andar => andar.GetProperty("unidades").GetArrayLength()));
        }

        var otherTenantResponse = await client.GetAsync($"/api/v1/condominios/{tenantB.CondominioId}/estrutura");
        Assert.Equal(HttpStatusCode.Forbidden, otherTenantResponse.StatusCode);
    }

    [Fact]
    public async Task CriarBloco_ShouldReturn201_ReflectTree_AndRejectDuplicateName()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);

        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos",
            new { nome = "Bloco B" });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);
        var created = await createResponse.Content.ReadFromJsonAsync<BlocoResponse>();
        Assert.NotNull(created);
        Assert.Equal(seeded.CondominioId, created!.CondominioId);
        Assert.Equal("Bloco B", created.Nome);
        Assert.True(created.Ativo);

        var estruturaResponse = await client.GetAsync($"/api/v1/condominios/{seeded.CondominioId}/estrutura");
        Assert.Equal(HttpStatusCode.OK, estruturaResponse.StatusCode);
        using (var estruturaJson = await EstruturaTestData.ReadJsonAsync(estruturaResponse))
        {
            var nomes = estruturaJson.RootElement.GetProperty("blocos")
                .EnumerateArray()
                .Select(bloco => bloco.GetProperty("nome").GetString())
                .ToArray();
            Assert.Contains("Bloco B", nomes);
        }

        var duplicateResponse = await client.PostAsJsonAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos",
            new { nome = "Bloco B" });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        var duplicateProblem = await duplicateResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(duplicateProblem);
        Assert.Equal("Conflito canônico", duplicateProblem!.Title);
    }

    [Fact]
    public async Task RenomearBloco_ShouldReturn200_AndRejectSameName()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);

        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var renameResponse = await client.PatchAsJsonAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}",
            new { nome = "Torre Alfa" });

        Assert.Equal(HttpStatusCode.OK, renameResponse.StatusCode);
        var renamed = await renameResponse.Content.ReadFromJsonAsync<BlocoResponse>();
        Assert.NotNull(renamed);
        Assert.Equal("Torre Alfa", renamed!.Nome);

        var invalidResponse = await client.PatchAsJsonAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}",
            new { nome = "Torre Alfa" });

        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        var invalidProblem = await invalidResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(invalidProblem);
        Assert.Equal("Falha de validação", invalidProblem!.Title);
    }

    [Fact]
    public async Task InativarBloco_ShouldHideByDefault_AndReturn422WhenAlreadyInactive()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);

        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var inativarResponse = await client.PostAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}:inativar",
            null);

        Assert.Equal(HttpStatusCode.OK, inativarResponse.StatusCode);
        var inativado = await inativarResponse.Content.ReadFromJsonAsync<BlocoResponse>();
        Assert.NotNull(inativado);
        Assert.False(inativado!.Ativo);
        Assert.NotNull(inativado.InativadoEm);

        var hiddenResponse = await client.GetAsync($"/api/v1/condominios/{seeded.CondominioId}/estrutura?includeInactive=false");
        using (var hiddenJson = await EstruturaTestData.ReadJsonAsync(hiddenResponse))
        {
            Assert.Empty(hiddenJson.RootElement.GetProperty("blocos").EnumerateArray());
        }

        var visibleResponse = await client.GetAsync($"/api/v1/condominios/{seeded.CondominioId}/estrutura?includeInactive=true");
        using (var visibleJson = await EstruturaTestData.ReadJsonAsync(visibleResponse))
        {
            var bloco = Assert.Single(visibleJson.RootElement.GetProperty("blocos").EnumerateArray());
            Assert.False(bloco.GetProperty("ativo").GetBoolean());
        }

        var secondAttemptResponse = await client.PostAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}:inativar",
            null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, secondAttemptResponse.StatusCode);
    }

    [Fact]
    public async Task ReativarBloco_ShouldReturn200_WhenInactive()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);
        await EstruturaTestData.ForceInactivateBlocoAsync(factory.Services, seeded.BlocoId!.Value, seeded.SindicoUserId);

        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var response = await client.PostAsync($"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}:reativar", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bloco = await response.Content.ReadFromJsonAsync<BlocoResponse>();
        Assert.NotNull(bloco);
        Assert.True(bloco!.Ativo);
        Assert.Null(bloco.InativadoEm);
    }

    [Fact]
    public async Task ReativarBloco_ShouldReturn409_WhenActiveDuplicateExists()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services, blocoNome: "Bloco A");
        await EstruturaTestData.ForceInactivateBlocoAsync(factory.Services, seeded.BlocoId!.Value, seeded.SindicoUserId);
        await EstruturaTestData.AddBlocoAsync(factory.Services, seeded.CondominioId, "Bloco A", seeded.SindicoUserId);

        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var response = await client.PostAsync($"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}:reativar", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ReativarBloco_ShouldReturn422_WhenAlreadyActive()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);

        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var response = await client.PostAsync($"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}:reativar", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CriarUnidade_ShouldReturn201_NormalizeValue_AndRejectDuplicateCanonical()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);

        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}/unidades",
            new { andar = 2, numero = "202a" });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var unidade = await createResponse.Content.ReadFromJsonAsync<UnidadeResponse>();
        Assert.NotNull(unidade);
        Assert.Equal("202A", unidade!.Numero);

        var duplicateResponse = await client.PostAsJsonAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}/unidades",
            new { andar = 2, numero = "202A" });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task CriarUnidade_ShouldReturn422_WhenBlockIsInactive()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);
        await EstruturaTestData.ForceInactivateBlocoAsync(factory.Services, seeded.BlocoId!.Value, seeded.SindicoUserId);

        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var response = await client.PostAsJsonAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}/unidades",
            new { andar = 3, numero = "301" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task InativarUnidade_ShouldReturn200_And422WhenAlreadyInactive()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);
        var unidadeId = seeded.Unidades[0].Id;

        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var firstResponse = await client.PostAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}/unidades/{unidadeId}:inativar",
            null);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var unidade = await firstResponse.Content.ReadFromJsonAsync<UnidadeResponse>();
        Assert.NotNull(unidade);
        Assert.False(unidade!.Ativo);

        var secondResponse = await client.PostAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}/unidades/{unidadeId}:inativar",
            null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, secondResponse.StatusCode);
    }

    [Fact]
    public async Task ReativarUnidade_ShouldReturn200_And409WhenCanonicalConflictExists()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);
        var unidadeId = seeded.Unidades[0].Id;

        await EstruturaTestData.ForceInactivateUnidadeAsync(factory.Services, unidadeId, seeded.SindicoUserId);

        using (var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId)))
        {
            var happyResponse = await client.PostAsync(
                $"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}/unidades/{unidadeId}:reativar",
                null);

            Assert.Equal(HttpStatusCode.OK, happyResponse.StatusCode);
        }

        await EstruturaTestData.ForceInactivateUnidadeAsync(factory.Services, unidadeId, seeded.SindicoUserId);
        await EstruturaTestData.AddUnidadeAsync(
            factory.Services,
            seeded.CondominioId,
            seeded.BlocoId!.Value,
            seeded.Unidades[0].Andar,
            seeded.Unidades[0].Numero,
            seeded.SindicoUserId);

        using var secondClient = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));
        var conflictResponse = await secondClient.PostAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}/unidades/{unidadeId}:reativar",
            null);

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
    }

    [Fact]
    public async Task ReativarUnidade_ShouldReturn404_WhenUnitDoesNotExist()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);

        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var response = await client.PostAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}/unidades/{Guid.NewGuid()}:reativar",
            null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReativarUnidade_ShouldReturn422_WhenParentBlockIsInactive()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);
        var unidadeId = seeded.Unidades[0].Id;

        await EstruturaTestData.ForceInactivateUnidadeAsync(factory.Services, unidadeId, seeded.SindicoUserId);
        await EstruturaTestData.ForceInactivateBlocoAsync(factory.Services, seeded.BlocoId!.Value, seeded.SindicoUserId);

        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var response = await client.PostAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}/unidades/{unidadeId}:reativar",
            null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetEstruturaAdmin_ShouldReturn200_ForOperator_And403ForSindico()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services, tenantName: "Residencial Operador");

        using var operatorClient = factory.CreateClient(TestAuthContext.Operator(seeded.OperatorUserId));
        var operatorResponse = await operatorClient.GetAsync($"/api/v1/admin/condominios/{seeded.CondominioId}/estrutura");
        Assert.Equal(HttpStatusCode.OK, operatorResponse.StatusCode);

        using var sindicoClient = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));
        var sindicoResponse = await sindicoClient.GetAsync($"/api/v1/admin/condominios/{seeded.CondominioId}/estrutura");
        Assert.Equal(HttpStatusCode.Forbidden, sindicoResponse.StatusCode);
    }

    [Fact]
    public async Task GetEstrutura_PerformanceInformal_With300Units_ShouldLogElapsedTime()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);

        var units = Enumerable.Range(0, 300)
            .Select(index => ((index / 10) + 1, $"{100 + index}"))
            .ToArray();
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services, unidades: units);

        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));
        var stopwatch = Stopwatch.StartNew();

        var response = await client.GetAsync($"/api/v1/condominios/{seeded.CondominioId}/estrutura");

        stopwatch.Stop();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        output.WriteLine($"GET /estrutura 300 unidades => {stopwatch.ElapsedMilliseconds} ms (target informal: < 500 ms)");
    }

    private sealed record BlocoResponse(Guid Id, Guid CondominioId, string Nome, bool Ativo, DateTimeOffset? InativadoEm);

    private sealed record UnidadeResponse(Guid Id, Guid BlocoId, int Andar, string Numero, bool Ativo, DateTimeOffset? InativadoEm);
}
