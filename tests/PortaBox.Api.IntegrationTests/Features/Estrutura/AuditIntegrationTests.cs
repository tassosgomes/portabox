using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Api.IntegrationTests.Helpers;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Api.IntegrationTests.Features.Estrutura;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class AuditIntegrationTests(PostgresDatabaseFixture fixture)
{
    [Fact]
    public async Task CriarBloco_ShouldCreateExactlyOneAuditEntry_WithExpectedMetadata()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);
        var beforeCount = (await EstruturaTestData.GetAuditEntriesAsync(factory.Services, seeded.CondominioId)).Count;
        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var response = await client.PostAsJsonAsync($"/api/v1/condominios/{seeded.CondominioId}/blocos", new { nome = "Bloco Audit" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<BlocoResponse>();
        Assert.NotNull(created);
        await AssertSingleNewAuditEntryAsync(
            factory,
            seeded.CondominioId,
            beforeCount,
            TenantAuditEventKind.BlocoCriado,
            metadata =>
            {
                Assert.Equal(created!.Id.ToString(), metadata.GetProperty("blocoId").GetString());
                Assert.Equal("Bloco Audit", metadata.GetProperty("nome").GetString());
            });
    }

    [Fact]
    public async Task RenomearBloco_ShouldCreateExactlyOneAuditEntry_WithExpectedMetadata()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services, blocoNome: "Bloco Inicial");
        var beforeCount = (await EstruturaTestData.GetAuditEntriesAsync(factory.Services, seeded.CondominioId)).Count;
        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}",
            new { nome = "Torre Audit" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertSingleNewAuditEntryAsync(
            factory,
            seeded.CondominioId,
            beforeCount,
            TenantAuditEventKind.BlocoRenomeado,
            metadata =>
            {
                Assert.Equal(seeded.BlocoId.ToString(), metadata.GetProperty("blocoId").GetString());
                Assert.Equal("Bloco Inicial", metadata.GetProperty("nomeAntes").GetString());
                Assert.Equal("Torre Audit", metadata.GetProperty("nomeDepois").GetString());
            });
    }

    [Fact]
    public async Task InativarBloco_ShouldCreateExactlyOneAuditEntry_WithExpectedMetadata()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services, blocoNome: "Bloco Audit");
        var beforeCount = (await EstruturaTestData.GetAuditEntriesAsync(factory.Services, seeded.CondominioId)).Count;
        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var response = await client.PostAsync($"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}:inativar", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertSingleNewAuditEntryAsync(
            factory,
            seeded.CondominioId,
            beforeCount,
            TenantAuditEventKind.BlocoInativado,
            metadata =>
            {
                Assert.Equal(seeded.BlocoId.ToString(), metadata.GetProperty("blocoId").GetString());
                Assert.Equal("Bloco Audit", metadata.GetProperty("nome").GetString());
            });
    }

    [Fact]
    public async Task ReativarBloco_ShouldCreateExactlyOneAuditEntry_WithExpectedMetadata()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services, blocoNome: "Bloco Audit");
        await EstruturaTestData.ForceInactivateBlocoAsync(factory.Services, seeded.BlocoId!.Value, seeded.SindicoUserId);
        var beforeCount = (await EstruturaTestData.GetAuditEntriesAsync(factory.Services, seeded.CondominioId)).Count;
        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var response = await client.PostAsync($"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}:reativar", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertSingleNewAuditEntryAsync(
            factory,
            seeded.CondominioId,
            beforeCount,
            TenantAuditEventKind.BlocoReativado,
            metadata =>
            {
                Assert.Equal(seeded.BlocoId.ToString(), metadata.GetProperty("blocoId").GetString());
                Assert.Equal("Bloco Audit", metadata.GetProperty("nome").GetString());
            });
    }

    [Fact]
    public async Task CriarUnidade_ShouldCreateExactlyOneAuditEntry_WithExpectedMetadata()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);
        var beforeCount = (await EstruturaTestData.GetAuditEntriesAsync(factory.Services, seeded.CondominioId)).Count;
        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var response = await client.PostAsJsonAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}/unidades",
            new { andar = 3, numero = "301A" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var unidade = await response.Content.ReadFromJsonAsync<UnidadeResponse>();
        Assert.NotNull(unidade);
        await AssertSingleNewAuditEntryAsync(
            factory,
            seeded.CondominioId,
            beforeCount,
            TenantAuditEventKind.UnidadeCriada,
            metadata =>
            {
                Assert.Equal(unidade!.Id.ToString(), metadata.GetProperty("unidadeId").GetString());
                Assert.Equal(seeded.BlocoId.ToString(), metadata.GetProperty("blocoId").GetString());
                Assert.Equal(3, metadata.GetProperty("andar").GetInt32());
                Assert.Equal("301A", metadata.GetProperty("numero").GetString());
            });
    }

    [Fact]
    public async Task InativarUnidade_ShouldCreateExactlyOneAuditEntry_WithExpectedMetadata()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);
        var targetUnit = seeded.Unidades[0];
        var beforeCount = (await EstruturaTestData.GetAuditEntriesAsync(factory.Services, seeded.CondominioId)).Count;
        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var response = await client.PostAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}/unidades/{targetUnit.Id}:inativar",
            null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertSingleNewAuditEntryAsync(
            factory,
            seeded.CondominioId,
            beforeCount,
            TenantAuditEventKind.UnidadeInativada,
            metadata =>
            {
                Assert.Equal(targetUnit.Id.ToString(), metadata.GetProperty("unidadeId").GetString());
                Assert.Equal(seeded.BlocoId.ToString(), metadata.GetProperty("blocoId").GetString());
                Assert.Equal(targetUnit.Andar, metadata.GetProperty("andar").GetInt32());
                Assert.Equal(targetUnit.Numero, metadata.GetProperty("numero").GetString());
            });
    }

    [Fact]
    public async Task ReativarUnidade_ShouldCreateExactlyOneAuditEntry_WithExpectedMetadata()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);
        var targetUnit = seeded.Unidades[0];
        await EstruturaTestData.ForceInactivateUnidadeAsync(factory.Services, targetUnit.Id, seeded.SindicoUserId);
        var beforeCount = (await EstruturaTestData.GetAuditEntriesAsync(factory.Services, seeded.CondominioId)).Count;
        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var response = await client.PostAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos/{seeded.BlocoId}/unidades/{targetUnit.Id}:reativar",
            null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertSingleNewAuditEntryAsync(
            factory,
            seeded.CondominioId,
            beforeCount,
            TenantAuditEventKind.UnidadeReativada,
            metadata =>
            {
                Assert.Equal(targetUnit.Id.ToString(), metadata.GetProperty("unidadeId").GetString());
                Assert.Equal(seeded.BlocoId.ToString(), metadata.GetProperty("blocoId").GetString());
                Assert.Equal(targetUnit.Andar, metadata.GetProperty("andar").GetInt32());
                Assert.Equal(targetUnit.Numero, metadata.GetProperty("numero").GetString());
            });
    }

    private static async Task AssertSingleNewAuditEntryAsync(
        EstruturaTestApiFactory factory,
        Guid tenantId,
        int beforeCount,
        TenantAuditEventKind expectedKind,
        Action<JsonElement> metadataAssertion)
    {
        var entries = await EstruturaTestData.GetAuditEntriesAsync(factory.Services, tenantId);
        Assert.Equal(beforeCount + 1, entries.Count);

        var entry = entries[^1];
        Assert.Equal(expectedKind, entry.EventKind);
        Assert.False(string.IsNullOrWhiteSpace(entry.MetadataJson));

        using var metadata = JsonDocument.Parse(entry.MetadataJson!);
        metadataAssertion(metadata.RootElement);
    }

    private sealed record BlocoResponse(Guid Id, Guid CondominioId, string Nome, bool Ativo, DateTimeOffset? InativadoEm);

    private sealed record UnidadeResponse(Guid Id, Guid BlocoId, int Andar, string Numero, bool Ativo, DateTimeOffset? InativadoEm);
}
