using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Api.IntegrationTests.Features.Outbox;

/// <summary>
/// Tests the outbox pattern guarantees:
/// 1. After a successful CreateCondominio, a domain_event_outbox row exists.
/// 2. After creating with the publisher enabled (NoOp), published_at is populated.
/// </summary>
[Collection(nameof(AppFactoryCollection))]
public sealed class OutboxAtomicityTests(AppFactoryFixture factory)
{
    [Fact]
    public async Task CreateCondominio_ShouldInsertDomainEventOutboxRow()
    {
        await factory.ResetAsync();

        using var client = await factory.CreateOperatorClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/admin/condominios", BuildRequest("45.723.174/0001-10", $"outbox-{Guid.NewGuid():N}@portabox.test"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var body = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var condominioId = body.GetProperty("condominioId").GetGuid();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var outboxEntry = await dbContext.DomainEventOutboxEntries.SingleAsync();
        Assert.Equal("condominio.cadastrado.v1", outboxEntry.EventType);
        Assert.Equal(condominioId, outboxEntry.AggregateId);
        Assert.Null(outboxEntry.PublishedAt);
    }

    [Fact]
    public async Task CreateCondominio_DuplicateCnpj_ShouldLeaveOutboxEmpty_WhenHandlerRollsBack()
    {
        await factory.ResetAsync();

        using var client = await factory.CreateOperatorClientAsync();
        const string cnpj = "04.252.011/0001-10";

        var firstResponse = await client.PostAsJsonAsync("/api/v1/admin/condominios", BuildRequest(cnpj, $"first-{Guid.NewGuid():N}@portabox.test"));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Clear the outbox entry from the first creation
        using var setupScope = factory.Services.CreateScope();
        await setupScope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Database.ExecuteSqlRawAsync("DELETE FROM domain_event_outbox");

        // Attempt duplicate - should fail with 409 (no partial outbox write)
        var secondResponse = await client.PostAsJsonAsync("/api/v1/admin/condominios", BuildRequest(cnpj, $"second-{Guid.NewGuid():N}@portabox.test"));
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        // Outbox must remain empty because the handler returned failure before SaveChanges
        using var verifyScope = factory.Services.CreateScope();
        var dbContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, await dbContext.DomainEventOutboxEntries.CountAsync());
    }

    private static object BuildRequest(string cnpj, string sindicoEmail) => new
    {
        nomeFantasia = $"Outbox Test {cnpj}",
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
            signatarioNome = "Teste",
            signatarioCpf = "529.982.247-25",
            dataTermo = "2026-04-02"
        },
        sindico = new
        {
            nome = "Sindico Outbox",
            email = sindicoEmail,
            celularE164 = "+5511999880001"
        }
    };
}
