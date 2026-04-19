using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Api.IntegrationTests.Features.MagicLinks;

/// <summary>
/// Tests: resend (only second link works), expiry (400 generic), go-live flow.
/// </summary>
[Collection(nameof(AppFactoryCollection))]
public sealed class MagicLinkFlowTests(AppFactoryFixture factory)
{
    [Fact]
    public async Task ResendMagicLink_TwoConsecutive_OnlySecondTokenWorks()
    {
        await factory.ResetAsync();
        await factory.ClearMailHogAsync();

        using var operatorClient = await factory.CreateOperatorClientAsync();
        var sindicoEmail = $"resend-{Guid.NewGuid():N}@portabox.test";
        var (condominioId, sindicoUserId) = await CreateCondominioAsync(operatorClient, "45.723.174/0001-10", sindicoEmail);

        // Get the original token from the first email
        await Task.Delay(200);
        var originalToken = await factory.GetLatestMagicLinkTokenAsync();

        // Resend: this invalidates the original and creates a new one
        await factory.ClearMailHogAsync();
        var resendResponse = await operatorClient.PostAsync(
            $"/api/v1/admin/condominios/{condominioId}/sindicos/{sindicoUserId}:resend-magic-link",
            null);
        Assert.Equal(HttpStatusCode.OK, resendResponse.StatusCode);

        await Task.Delay(200);
        var newToken = await factory.GetLatestMagicLinkTokenAsync();
        Assert.NotEqual(originalToken, newToken);

        // Original token should now be rejected (invalidated)
        using var publicClient = factory.CreateClient();
        var originalSetupResponse = await publicClient.PostAsJsonAsync("/api/v1/auth/password-setup", new
        {
            token = originalToken,
            password = "Sindico@Pass123"
        });
        Assert.Equal(HttpStatusCode.BadRequest, originalSetupResponse.StatusCode);

        // New token should work
        var newSetupResponse = await publicClient.PostAsJsonAsync("/api/v1/auth/password-setup", new
        {
            token = newToken,
            password = "Sindico@Pass123"
        });
        Assert.Equal(HttpStatusCode.OK, newSetupResponse.StatusCode);
    }

    [Fact]
    public async Task ExpiredMagicLink_ShouldReturn400Generic()
    {
        await factory.ResetAsync();
        await factory.ClearMailHogAsync();

        using var operatorClient = await factory.CreateOperatorClientAsync();
        var sindicoEmail = $"expired-{Guid.NewGuid():N}@portabox.test";
        var (condominioId, sindicoUserId) = await CreateCondominioAsync(operatorClient, "04.252.011/0001-10", sindicoEmail);

        await Task.Delay(200);
        var token = await factory.GetLatestMagicLinkTokenAsync();

        // Force the magic link to be expired by updating expires_at in the DB
        await ForceExpireMagicLinkAsync();

        using var publicClient = factory.CreateClient();
        var setupResponse = await publicClient.PostAsJsonAsync("/api/v1/auth/password-setup", new
        {
            token,
            password = "Sindico@Pass123"
        });

        // Must return 400 (generic — no distinction between expired, consumed, invalid)
        Assert.Equal(HttpStatusCode.BadRequest, setupResponse.StatusCode);
    }

    [Fact]
    public async Task ActivateCondominio_FromPreAtivo_ShouldTransitionToAtivoWithAuditEntry()
    {
        await factory.ResetAsync();

        using var operatorClient = await factory.CreateOperatorClientAsync();
        var sindicoEmail = $"activate-{Guid.NewGuid():N}@portabox.test";
        var (condominioId, _) = await CreateCondominioAsync(operatorClient, "12.345.678/0001-95", sindicoEmail);

        // Verify initial state = PreAtivo (1)
        var detailsBefore = await operatorClient.GetAsync($"/api/v1/admin/condominios/{condominioId}");
        var detailsBodyBefore = await detailsBefore.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, detailsBodyBefore.GetProperty("status").GetInt32());

        // Activate
        var activateResponse = await operatorClient.PostAsJsonAsync(
            $"/api/v1/admin/condominios/{condominioId}:activate",
            new { note = "Go-live test" });
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);

        // Verify state = Ativo (2) + audit log
        var detailsAfter = await operatorClient.GetAsync($"/api/v1/admin/condominios/{condominioId}");
        var detailsBodyAfter = await detailsAfter.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, detailsBodyAfter.GetProperty("status").GetInt32());
        Assert.NotNull(detailsBodyAfter.GetProperty("activatedAt").GetString());

        var auditLog = detailsBodyAfter.GetProperty("auditLog");
        Assert.True(auditLog.GetArrayLength() >= 2); // Created + Activated
        var activatedEntry = auditLog.EnumerateArray()
            .FirstOrDefault(e => e.GetProperty("eventKind").GetInt32() == 2); // 2 = Activated
        Assert.NotEqual(default, activatedEntry);
    }

    // ─── helpers ──────────────────────────────────────────────────────────────

    private async Task ForceExpireMagicLinkAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var magicLink = await dbContext.MagicLinks.SingleAsync();
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE magic_link SET expires_at = {DateTimeOffset.UtcNow.AddMinutes(-10)} WHERE id = {magicLink.Id}");
    }

    private static async Task<(Guid CondominioId, Guid SindicoUserId)> CreateCondominioAsync(
        HttpClient client,
        string cnpj,
        string sindicoEmail)
    {
        var response = await client.PostAsJsonAsync("/api/v1/admin/condominios", new
        {
            nomeFantasia = $"Cond {cnpj}",
            cnpj,
            enderecoLogradouro = (string?)null,
            enderecoNumero = (string?)null,
            enderecoComplemento = (string?)null,
            enderecoBairro = (string?)null,
            enderecoCidade = (string?)null,
            enderecoUf = (string?)null,
            enderecoCep = (string?)null,
            administradoraNome = (string?)null,
            optIn = new
            {
                dataAssembleia = "2026-04-01",
                quorumDescricao = "Maioria simples",
                signatarioNome = "Nome Teste",
                signatarioCpf = "529.982.247-25",
                dataTermo = "2026-04-02"
            },
            sindico = new
            {
                nome = "Sindico Teste",
                email = sindicoEmail,
                celularE164 = "+5511999880001"
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("condominioId").GetGuid(), body.GetProperty("sindicoUserId").GetGuid());
    }
}
