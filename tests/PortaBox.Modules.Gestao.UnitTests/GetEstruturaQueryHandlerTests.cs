using Moq;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Modules.Gestao.Application.Blocos;
using PortaBox.Modules.Gestao.Application.Estrutura;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Application.Unidades;
using PortaBox.Modules.Gestao.Domain.Blocos;
using PortaBox.Modules.Gestao.Domain;
using PortaBox.Modules.Gestao.Domain.Unidades;

namespace PortaBox.Modules.Gestao.UnitTests;

public sealed class GetEstruturaQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_HappyPath_ShouldReturnOrderedTree()
    {
        var condominio = CreateCondominio();
        var tenantContext = CreateTenantContext(condominio.Id);
        var blocoA = CreateBloco(condominio.Id, condominio.Id, "Bloco A");
        var blocoB = CreateBloco(condominio.Id, condominio.Id, "Torre B");

        var handler = CreateHandler(
            condominio,
            tenantContext,
            [blocoB, blocoA],
            [
                CreateUnidade(condominio.Id, blocoA.Id, 3, "302"),
                CreateUnidade(condominio.Id, blocoA.Id, 1, "102"),
                CreateUnidade(condominio.Id, blocoA.Id, 1, "101"),
                CreateUnidade(condominio.Id, blocoA.Id, 2, "201"),
                CreateUnidade(condominio.Id, blocoA.Id, 3, "301"),
                CreateUnidade(condominio.Id, blocoA.Id, 2, "202"),
                CreateUnidade(condominio.Id, blocoB.Id, 2, "201"),
                CreateUnidade(condominio.Id, blocoB.Id, 1, "101"),
                CreateUnidade(condominio.Id, blocoB.Id, 3, "302"),
                CreateUnidade(condominio.Id, blocoB.Id, 1, "102"),
                CreateUnidade(condominio.Id, blocoB.Id, 2, "202"),
                CreateUnidade(condominio.Id, blocoB.Id, 3, "301")
            ],
            new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));

        var result = await handler.HandleAsync(new GetEstruturaQuery(condominio.Id, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(condominio.Id, result.Value!.CondominioId);
        Assert.Equal(condominio.NomeFantasia, result.Value.NomeFantasia);
        Assert.Collection(
            result.Value.Blocos,
            bloco =>
            {
                Assert.Equal("Bloco A", bloco.Nome);
                Assert.Collection(
                    bloco.Andares,
                    andar => Assert.Equal(1, andar.Andar),
                    andar => Assert.Equal(2, andar.Andar),
                    andar => Assert.Equal(3, andar.Andar));
            },
            bloco => Assert.Equal("Torre B", bloco.Nome));
        Assert.Equal(new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc), result.Value.GeradoEm);
    }

    [Fact]
    public async Task HandleAsync_ShouldOrderBlocosAlphabetically()
    {
        var condominio = CreateCondominio();
        var handler = CreateHandler(
            condominio,
            CreateTenantContext(condominio.Id),
            [
                CreateBloco(condominio.Id, condominio.Id, "Torre B"),
                CreateBloco(condominio.Id, condominio.Id, "Bloco A"),
                CreateBloco(condominio.Id, condominio.Id, "Torre A")
            ],
            [],
            new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));

        var result = await handler.HandleAsync(new GetEstruturaQuery(condominio.Id, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Bloco A", "Torre A", "Torre B"], result.Value!.Blocos.Select(bloco => bloco.Nome).ToArray());
    }

    [Fact]
    public async Task HandleAsync_ShouldOrderAndaresNumerically()
    {
        var condominio = CreateCondominio();
        var bloco = CreateBloco(condominio.Id, condominio.Id, "Bloco A");
        var handler = CreateHandler(
            condominio,
            CreateTenantContext(condominio.Id),
            [bloco],
            [
                CreateUnidade(condominio.Id, bloco.Id, 3, "301"),
                CreateUnidade(condominio.Id, bloco.Id, 1, "101"),
                CreateUnidade(condominio.Id, bloco.Id, 10, "1001"),
                CreateUnidade(condominio.Id, bloco.Id, 2, "201")
            ],
            new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));

        var result = await handler.HandleAsync(new GetEstruturaQuery(condominio.Id, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([1, 2, 3, 10], result.Value!.Blocos.Single().Andares.Select(andar => andar.Andar).ToArray());
    }

    [Fact]
    public async Task HandleAsync_ShouldOrderUnidadesSemantically()
    {
        var condominio = CreateCondominio();
        var bloco = CreateBloco(condominio.Id, condominio.Id, "Bloco A");
        var handler = CreateHandler(
            condominio,
            CreateTenantContext(condominio.Id),
            [bloco],
            [
                CreateUnidade(condominio.Id, bloco.Id, 1, "102"),
                CreateUnidade(condominio.Id, bloco.Id, 1, "101A"),
                CreateUnidade(condominio.Id, bloco.Id, 1, "99"),
                CreateUnidade(condominio.Id, bloco.Id, 1, "101")
            ],
            new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));

        var result = await handler.HandleAsync(new GetEstruturaQuery(condominio.Id, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["99", "101", "101A", "102"], result.Value!.Blocos.Single().Andares.Single().Unidades.Select(unidade => unidade.Numero).ToArray());
    }

    [Fact]
    public async Task HandleAsync_WhenIncludeInactiveIsFalse_ShouldOmitInactiveItems()
    {
        var condominio = CreateCondominio();
        var activeBloco = CreateBloco(condominio.Id, condominio.Id, "Bloco A");
        var inactiveBloco = CreateInactiveBloco(condominio.Id, condominio.Id, "Bloco B");
        var activeUnidade = CreateUnidade(condominio.Id, activeBloco.Id, 1, "101");
        var inactiveUnidade = CreateInactiveUnidade(condominio.Id, activeBloco.Id, 1, "102");

        var handler = CreateHandler(
            condominio,
            CreateTenantContext(condominio.Id),
            [activeBloco],
            [activeUnidade],
            new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero),
            includeInactiveAssertion: includeInactive => Assert.False(includeInactive));

        var result = await handler.HandleAsync(new GetEstruturaQuery(condominio.Id, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var bloco = result.Value!.Blocos.Single();
        Assert.Equal(activeBloco.Id, bloco.Id);
        Assert.DoesNotContain(result.Value.Blocos, current => current.Id == inactiveBloco.Id);
        Assert.Equal([activeUnidade.Numero], bloco.Andares.Single().Unidades.Select(unidade => unidade.Numero).ToArray());
        Assert.DoesNotContain(bloco.Andares.Single().Unidades, unidade => unidade.Numero == inactiveUnidade.Numero);
    }

    [Fact]
    public async Task HandleAsync_WhenIncludeInactiveIsTrue_ShouldIncludeInactiveWithoutCrossTenantLeak()
    {
        var condominio = CreateCondominio();
        var sameTenantBloco = CreateInactiveBloco(condominio.Id, condominio.Id, "Bloco A");
        var foreignTenantBloco = CreateBloco(Guid.NewGuid(), Guid.NewGuid(), "Bloco Invasor");
        var sameTenantUnidade = CreateInactiveUnidade(condominio.Id, sameTenantBloco.Id, 1, "101");
        var foreignTenantUnidade = CreateUnidade(foreignTenantBloco.TenantId, foreignTenantBloco.Id, 1, "999");

        var handler = CreateHandler(
            condominio,
            CreateTenantContext(condominio.Id),
            [sameTenantBloco, foreignTenantBloco],
            [sameTenantUnidade, foreignTenantUnidade],
            new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero),
            includeInactiveAssertion: includeInactive => Assert.True(includeInactive));

        var result = await handler.HandleAsync(new GetEstruturaQuery(condominio.Id, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var bloco = result.Value!.Blocos.Single();
        Assert.Equal(sameTenantBloco.Id, bloco.Id);
        Assert.Single(bloco.Andares.Single().Unidades);
        Assert.Equal(sameTenantUnidade.Id, bloco.Andares.Single().Unidades.Single().Id);
    }

    [Fact]
    public async Task HandleAsync_WhenCondominioDoesNotExist_ShouldReturnFailure()
    {
        var repository = new Mock<ICondominioRepository>();
        repository
            .Setup(current => current.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Condominio?)null);

        var handler = new GetEstruturaQueryHandler(
            repository.Object,
            new Mock<IBlocoRepository>().Object,
            new Mock<IUnidadeRepository>().Object,
            CreateTenantContext(Guid.NewGuid()),
            TimeProvider.System);

        var result = await handler.HandleAsync(new GetEstruturaQuery(Guid.NewGuid(), false), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Condominio nao encontrado", result.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenTenantScopeDoesNotMatchCondominio_ShouldReturnFailure()
    {
        var condominio = CreateCondominio();
        var handler = CreateHandler(
            condominio,
            CreateTenantContext(Guid.NewGuid()),
            [CreateBloco(condominio.Id, condominio.Id, "Bloco A")],
            [],
            new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));

        var result = await handler.HandleAsync(new GetEstruturaQuery(condominio.Id, true), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Condominio nao encontrado", result.Error);
    }

    [Fact]
    public async Task HandleAsync_ShouldPopulateGeradoEmWithClockUtcNow()
    {
        var condominio = CreateCondominio();
        var now = new DateTimeOffset(2026, 4, 20, 16, 30, 0, TimeSpan.Zero);
        var handler = CreateHandler(
            condominio,
            CreateTenantContext(condominio.Id),
            [],
            [],
            now);

        var result = await handler.HandleAsync(new GetEstruturaQuery(condominio.Id, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(now.UtcDateTime, result.Value!.GeradoEm);
    }

    private static GetEstruturaQueryHandler CreateHandler(
        Condominio condominio,
        ITenantContext tenantContext,
        IReadOnlyList<Bloco> blocos,
        IReadOnlyList<Unidade> unidades,
        DateTimeOffset now,
        Action<bool>? includeInactiveAssertion = null)
    {
        var condominioRepository = new Mock<ICondominioRepository>();
        condominioRepository
            .Setup(current => current.GetByIdAsync(condominio.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condominio);

        var blocoRepository = new Mock<IBlocoRepository>();
        blocoRepository
            .Setup(current => current.ListByCondominioAsync(condominio.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, bool, CancellationToken>((_, includeInactive, _) => includeInactiveAssertion?.Invoke(includeInactive))
            .ReturnsAsync(blocos);

        var unidadeRepository = new Mock<IUnidadeRepository>();
        unidadeRepository
            .Setup(current => current.ListByCondominioAsync(condominio.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(unidades);

        return new GetEstruturaQueryHandler(
            condominioRepository.Object,
            blocoRepository.Object,
            unidadeRepository.Object,
            tenantContext,
            new FixedTimeProvider(now));
    }

    private static ITenantContext CreateTenantContext(Guid tenantId)
    {
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.SetupGet(current => current.TenantId).Returns(tenantId);
        return tenantContext.Object;
    }

    private static Condominio CreateCondominio()
    {
        return Condominio.Create(
            Guid.NewGuid(),
            "Residencial Alfa",
            "11222333000181",
            Guid.NewGuid(),
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 20, 10, 0, 0, TimeSpan.Zero)));
    }

    private static Bloco CreateBloco(Guid tenantId, Guid condominioId, string nome)
    {
        return Bloco.Create(
            Guid.NewGuid(),
            tenantId,
            condominioId,
            nome,
            Guid.NewGuid(),
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 20, 10, 0, 0, TimeSpan.Zero))).Value!;
    }

    private static Bloco CreateInactiveBloco(Guid tenantId, Guid condominioId, string nome)
    {
        var bloco = CreateBloco(tenantId, condominioId, nome);
        bloco.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 11, 0, 0, DateTimeKind.Utc));
        return bloco;
    }

    private static Unidade CreateUnidade(Guid tenantId, Guid blocoId, int andar, string numero)
    {
        var bloco = Bloco.Create(
            blocoId,
            tenantId,
            tenantId,
            "Bloco Auxiliar",
            Guid.NewGuid(),
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 20, 10, 0, 0, TimeSpan.Zero))).Value!;

        return Unidade.Create(
            Guid.NewGuid(),
            tenantId,
            bloco,
            andar,
            numero,
            Guid.NewGuid(),
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 20, 10, 5, 0, TimeSpan.Zero))).Value!;
    }

    private static Unidade CreateInactiveUnidade(Guid tenantId, Guid blocoId, int andar, string numero)
    {
        var unidade = CreateUnidade(tenantId, blocoId, andar, numero);
        unidade.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 11, 30, 0, DateTimeKind.Utc));
        return unidade;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
