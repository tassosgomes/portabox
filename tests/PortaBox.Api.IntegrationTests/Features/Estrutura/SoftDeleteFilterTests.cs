using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao.Application.Blocos;
using PortaBox.Modules.Gestao.Application.Unidades;

namespace PortaBox.Api.IntegrationTests.Features.Estrutura;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class SoftDeleteFilterTests(PostgresDatabaseFixture fixture)
{
    [Fact]
    public async Task DefaultQueries_ShouldOmitInactiveBlocosAndUnidades()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);
        var inactiveUnitId = seeded.Unidades[0].Id;

        await EstruturaTestData.ForceInactivateBlocoAsync(factory.Services, seeded.BlocoId!.Value, seeded.SindicoUserId);
        await EstruturaTestData.ForceInactivateUnidadeAsync(factory.Services, inactiveUnitId, seeded.SindicoUserId);

        await using var scope = factory.Services.CreateAsyncScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        using var tenantScope = tenantContext.BeginScope(seeded.CondominioId);

        var blocoRepository = scope.ServiceProvider.GetRequiredService<IBlocoRepository>();
        var unidadeRepository = scope.ServiceProvider.GetRequiredService<IUnidadeRepository>();

        var bloco = await blocoRepository.GetByIdAsync(seeded.BlocoId.Value);
        var unidade = await unidadeRepository.GetByIdAsync(inactiveUnitId);
        var blocos = await blocoRepository.ListByCondominioAsync(seeded.CondominioId, includeInactive: false, CancellationToken.None);
        var unidades = await unidadeRepository.ListByCondominioAsync(seeded.CondominioId, includeInactive: false, CancellationToken.None);

        Assert.Null(bloco);
        Assert.Null(unidade);
        Assert.Empty(blocos);
        Assert.Empty(unidades);
    }

    [Fact]
    public async Task IgnoreQueryFilters_ShouldReturnInactiveRows_WhenExplicitlyRequested()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);
        var inactiveUnitId = seeded.Unidades[0].Id;

        await EstruturaTestData.ForceInactivateBlocoAsync(factory.Services, seeded.BlocoId!.Value, seeded.SindicoUserId);
        await EstruturaTestData.ForceInactivateUnidadeAsync(factory.Services, inactiveUnitId, seeded.SindicoUserId);

        await using var scope = factory.Services.CreateAsyncScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        using var tenantScope = tenantContext.BeginScope(seeded.CondominioId);
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var bloco = await dbContext.Blocos.IgnoreQueryFilters().SingleAsync(current => current.Id == seeded.BlocoId.Value);
        var unidade = await dbContext.Unidades.IgnoreQueryFilters().SingleAsync(current => current.Id == inactiveUnitId);

        Assert.False(bloco.Ativo);
        Assert.False(unidade.Ativo);
    }
}
