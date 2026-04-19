using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PortaBox.Api.IntegrationTests.Fixtures;

namespace PortaBox.Api.IntegrationTests.Features.Condominios;

/// <summary>
/// Full E2E wizard flow: POST condominio → e-mail in MailHog → consume magic link → sindico logs in.
/// </summary>
[Collection(nameof(AppFactoryCollection))]
public sealed class CreateCondominioEndToEndTests(AppFactoryFixture factory)
{
    [Fact]
    public async Task FullWizardFlow_ShouldCreateCondominio_SendEmail_AndAllowSindicoLogin()
    {
        await factory.ResetAsync();
        await factory.ClearMailHogAsync();

        using var client = await factory.CreateOperatorClientAsync();
        var sindicoEmail = $"sindico-{Guid.NewGuid():N}@portabox.test";

        // Step 1: Create condominio via API
        var createResponse = await client.PostAsJsonAsync("/api/v1/admin/condominios", new
        {
            nomeFantasia = "Residencial E2E",
            cnpj = "45.723.174/0001-10",
            enderecoLogradouro = "Rua Teste",
            enderecoNumero = "100",
            enderecoComplemento = (string?)null,
            enderecoBairro = "Centro",
            enderecoCidade = "São Paulo",
            enderecoUf = "SP",
            enderecoCep = "01310100",
            administradoraNome = (string?)null,
            optIn = new
            {
                dataAssembleia = "2026-04-01",
                quorumDescricao = "Maioria simples",
                signatarioNome = "Carlos da Silva",
                signatarioCpf = "529.982.247-25",
                dataTermo = "2026-04-02"
            },
            sindico = new
            {
                nome = "Ana Oliveira",
                email = sindicoEmail,
                celularE164 = "+5511999880001"
            }
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var condominioId = createBody.GetProperty("condominioId").GetGuid();
        var sindicoUserId = createBody.GetProperty("sindicoUserId").GetGuid();

        Assert.NotEqual(Guid.Empty, condominioId);
        Assert.NotEqual(Guid.Empty, sindicoUserId);
        Assert.Contains($"/api/v1/admin/condominios/{condominioId}", createResponse.Headers.Location?.ToString());

        // Step 2: Verify magic link token arrived in MailHog
        var token = await factory.GetLatestMagicLinkTokenAsync();
        Assert.False(string.IsNullOrWhiteSpace(token));

        // Step 3: Consume magic link to set password
        using var publicClient = factory.CreateClient();
        var setupResponse = await publicClient.PostAsJsonAsync("/api/v1/auth/password-setup", new
        {
            token,
            password = "Sindico@Pass123"
        });
        Assert.Equal(HttpStatusCode.OK, setupResponse.StatusCode);

        // Step 4: Sindico can log in with the new password
        var loginResponse = await publicClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = sindicoEmail,
            password = "Sindico@Pass123"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Sindico", loginBody.GetProperty("role").GetString());
        Assert.Equal(condominioId, loginBody.GetProperty("tenantId").GetGuid());
    }

    [Fact]
    public async Task CreateCondominio_WithDetails_ShouldBeVisibleInDetailsEndpoint()
    {
        await factory.ResetAsync();

        using var client = await factory.CreateOperatorClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/admin/condominios", new
        {
            nomeFantasia = "Residencial Detalhes",
            cnpj = "04.252.011/0001-10",
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
                signatarioNome = "João Lima",
                signatarioCpf = "529.982.247-25",
                dataTermo = "2026-04-02"
            },
            sindico = new
            {
                nome = "Maria Costa",
                email = $"maria-{Guid.NewGuid():N}@portabox.test",
                celularE164 = "+5511999880002"
            }
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var condominioId = createBody.GetProperty("condominioId").GetGuid();

        var detailsResponse = await client.GetAsync($"/api/v1/admin/condominios/{condominioId}");
        Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);

        var details = await detailsResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Residencial Detalhes", details.GetProperty("nomeFantasia").GetString());
        Assert.Equal(1, details.GetProperty("status").GetInt32()); // PreAtivo = 1
    }
}
