using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PortaBox.Api.IntegrationTests.Fixtures;

namespace PortaBox.Api.IntegrationTests.Features.Condominios;

/// <summary>
/// Tests CNPJ deduplication: creating a tenant with an existing CNPJ must return 409.
/// </summary>
[Collection(nameof(AppFactoryCollection))]
public sealed class CnpjDeduplicationTests(AppFactoryFixture factory)
{
    [Fact]
    public async Task CreateCondominio_DuplicateCnpj_ShouldReturn409WithDetails()
    {
        await factory.ResetAsync();

        using var client = await factory.CreateOperatorClientAsync();
        const string cnpj = "12.345.678/0001-95";

        var firstResponse = await client.PostAsJsonAsync("/api/v1/admin/condominios", BuildRequest(cnpj, "sindico1"));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Second request with the same CNPJ should fail with 409
        var secondResponse = await client.PostAsJsonAsync("/api/v1/admin/condominios", BuildRequest(cnpj, "sindico2"));
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var problem = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("CNPJ já cadastrado.", problem.GetProperty("title").GetString());
    }

    private static object BuildRequest(string cnpj, string sindicoSuffix) => new
    {
        nomeFantasia = $"Cond {sindicoSuffix}",
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
            nome = $"Sindico {sindicoSuffix}",
            email = $"{sindicoSuffix}-{Guid.NewGuid():N}@portabox.test",
            celularE164 = "+5511999880001"
        }
    };
}
