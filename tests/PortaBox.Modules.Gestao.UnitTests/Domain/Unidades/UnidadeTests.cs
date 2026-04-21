using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.MultiTenancy;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Infrastructure.Repositories;
using PortaBox.Modules.Gestao.Domain;
using PortaBox.Modules.Gestao.Domain.Blocos;
using PortaBox.Modules.Gestao.Domain.Unidades;
using PortaBox.Modules.Gestao.Domain.Unidades.Events;

namespace PortaBox.Modules.Gestao.UnitTests.Domain.Unidades;

public sealed class UnidadeTests
{
    [Fact]
    public void Create_WithValidCanonicalData_ShouldReturnSuccessAndRaiseCreatedEvent()
    {
        var bloco = CreateBloco();
        var now = new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero);

        var result = Unidade.Create(
            Guid.NewGuid(),
            bloco.TenantId,
            bloco,
            2,
            "201",
            Guid.NewGuid(),
            new FakeTimeProvider(now));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Andar);
        Assert.Equal("201", result.Value.Numero);

        var domainEvent = Assert.IsType<UnidadeCriadaV1>(Assert.Single(result.Value.DomainEvents));
        Assert.Equal(result.Value.Id, domainEvent.UnidadeId);
        Assert.Equal(result.Value.BlocoId, domainEvent.BlocoId);
        Assert.Equal(2, domainEvent.Andar);
        Assert.Equal("201", domainEvent.Numero);
        Assert.Equal(result.Value.CriadoEm, domainEvent.OccurredAt);
    }

    [Fact]
    public void Create_ShouldNormalizeNumeroToUppercaseBeforePersisting()
    {
        var bloco = CreateBloco();

        var result = Unidade.Create(
            Guid.NewGuid(),
            bloco.TenantId,
            bloco,
            1,
            "101a",
            Guid.NewGuid(),
            TimeProvider.System);

        Assert.True(result.IsSuccess);
        Assert.Equal("101A", result.Value!.Numero);
    }

    [Theory]
    [InlineData("1AB")]
    [InlineData("12345")]
    [InlineData("20000")]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidNumero_ShouldReturnFailure(string numero)
    {
        var bloco = CreateBloco();

        var result = Unidade.Create(
            Guid.NewGuid(),
            bloco.TenantId,
            bloco,
            1,
            numero,
            Guid.NewGuid(),
            TimeProvider.System);

        Assert.False(result.IsSuccess);
        Assert.Equal("O numero da unidade deve seguir o formato de 1 a 4 digitos com sufixo alfabetico opcional.", result.Error);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Create_WithNegativeFloor_ShouldReturnFailure()
    {
        var bloco = CreateBloco();

        var result = Unidade.Create(
            Guid.NewGuid(),
            bloco.TenantId,
            bloco,
            -1,
            "101",
            Guid.NewGuid(),
            TimeProvider.System);

        Assert.False(result.IsSuccess);
        Assert.Equal("O andar da unidade deve ser maior ou igual a zero.", result.Error);
    }

    [Fact]
    public void Create_WithInactiveBloco_ShouldReturnFailure()
    {
        var bloco = CreateBloco();
        bloco.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 12, 30, 0, DateTimeKind.Utc));

        var result = Unidade.Create(
            Guid.NewGuid(),
            bloco.TenantId,
            bloco,
            1,
            "101",
            Guid.NewGuid(),
            TimeProvider.System);

        Assert.False(result.IsSuccess);
        Assert.Equal("Nao e possivel criar unidade em bloco inativo.", result.Error);
    }

    [Fact]
    public void Create_WithMismatchedTenant_ShouldReturnFailure()
    {
        var bloco = CreateBloco();

        var result = Unidade.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            bloco,
            1,
            "101",
            Guid.NewGuid(),
            TimeProvider.System);

        Assert.False(result.IsSuccess);
        Assert.Equal("Inconsistencia de tenant entre bloco e unidade.", result.Error);
    }

    [Fact]
    public void InativarAndReativar_ShouldRaiseExpectedEventsWithCanonicalPayload()
    {
        var unidade = CreateUnidade();

        var inativarResult = unidade.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 13, 0, 0, DateTimeKind.Utc));
        var reativarResult = unidade.Reativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        Assert.True(inativarResult.IsSuccess);
        Assert.True(reativarResult.IsSuccess);

        var inactivatedEvent = Assert.IsType<UnidadeInativadaV1>(unidade.DomainEvents[^2]);
        Assert.Equal(unidade.BlocoId, inactivatedEvent.BlocoId);
        Assert.Equal(unidade.Andar, inactivatedEvent.Andar);
        Assert.Equal(unidade.Numero, inactivatedEvent.Numero);

        var reactivatedEvent = Assert.IsType<UnidadeReativadaV1>(unidade.DomainEvents[^1]);
        Assert.Equal(unidade.BlocoId, reactivatedEvent.BlocoId);
        Assert.Equal(unidade.Andar, reactivatedEvent.Andar);
        Assert.Equal(unidade.Numero, reactivatedEvent.Numero);
    }

    [Fact]
    public async Task FindActiveByCanonicalAsync_ShouldReturnNullWhenMatchingUnitIsInactive()
    {
        var tenantId = Guid.NewGuid();
        var condominioId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var dbContext = CreateSqliteDbContext(tenantId);
        await SeedRequiredForeignKeysAsync(dbContext, condominioId, userId);
        var repository = new UnidadeRepository(dbContext);

        var bloco = CreateBloco(tenantId, condominioId, userId);
        var unidade = CreateUnidade(bloco: bloco, userId: userId);
        unidade.Inativar(userId, new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        dbContext.Blocos.Add(bloco);
        await repository.AddAsync(unidade);
        await repository.SaveAsync();

        var loaded = await repository.FindActiveByCanonicalAsync(tenantId, bloco.Id, unidade.Andar, unidade.Numero);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetByIdIncludingInactiveAsync_ShouldReturnInactiveUnit()
    {
        var tenantId = Guid.NewGuid();
        var condominioId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var dbContext = CreateSqliteDbContext(tenantId);
        await SeedRequiredForeignKeysAsync(dbContext, condominioId, userId);
        var repository = new UnidadeRepository(dbContext);

        var bloco = CreateBloco(tenantId, condominioId, userId);
        var unidade = CreateUnidade(bloco: bloco, userId: userId);
        unidade.Inativar(userId, new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        dbContext.Blocos.Add(bloco);
        await repository.AddAsync(unidade);
        await repository.SaveAsync();

        var loaded = await repository.GetByIdIncludingInactiveAsync(unidade.Id);

        Assert.NotNull(loaded);
        Assert.Equal(unidade.Id, loaded.Id);
        Assert.False(loaded.Ativo);
    }

    [Fact]
    public async Task ExistsActiveWithCanonicalAsync_ShouldNormalizeInputAndIgnoreInactiveUnit()
    {
        var tenantId = Guid.NewGuid();
        var condominioId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var dbContext = CreateSqliteDbContext(tenantId);
        await SeedRequiredForeignKeysAsync(dbContext, condominioId, userId);
        var repository = new UnidadeRepository(dbContext);

        var bloco = CreateBloco(tenantId, condominioId, userId);
        var unidade = CreateUnidade(bloco: bloco, userId: userId, andar: 10, numero: "101A");

        dbContext.Blocos.Add(bloco);
        await repository.AddAsync(unidade);
        await repository.SaveAsync();

        var existsWhenActive = await repository.ExistsActiveWithCanonicalAsync(tenantId, bloco.Id, 10, " 101a ");

        unidade.Inativar(userId, new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));
        await repository.SaveAsync();

        var existsWhenInactive = await repository.ExistsActiveWithCanonicalAsync(tenantId, bloco.Id, 10, "101A");

        Assert.True(existsWhenActive);
        Assert.False(existsWhenInactive);
    }

    [Fact]
    public async Task ListByBlocoAsync_WithIncludeInactiveFalse_ShouldReturnOnlyActiveUnitsOrdered()
    {
        var tenantId = Guid.NewGuid();
        var condominioId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var dbContext = CreateSqliteDbContext(tenantId);
        await SeedRequiredForeignKeysAsync(dbContext, condominioId, userId);
        var repository = new UnidadeRepository(dbContext);

        var bloco = CreateBloco(tenantId, condominioId, userId);
        var unidadePrimeiroAndar = CreateUnidade(bloco: bloco, userId: userId, andar: 1, numero: "102");
        var unidadeSegundoAndar = CreateUnidade(bloco: bloco, userId: userId, andar: 2, numero: "201");
        var unidadeInativa = CreateUnidade(bloco: bloco, userId: userId, andar: 1, numero: "101A");
        unidadeInativa.Inativar(userId, new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        dbContext.Blocos.Add(bloco);
        await repository.AddAsync(unidadeSegundoAndar);
        await repository.AddAsync(unidadeInativa);
        await repository.AddAsync(unidadePrimeiroAndar);
        await repository.SaveAsync();

        var unidades = await repository.ListByBlocoAsync(bloco.Id, includeInactive: false);

        Assert.Equal(2, unidades.Count);
        Assert.Equal(["102", "201"], unidades.Select(unidade => unidade.Numero).ToArray());
        Assert.All(unidades, unidade => Assert.True(unidade.Ativo));
    }

    [Fact]
    public async Task ListByBlocoAsync_WithIncludeInactiveTrue_ShouldReturnActiveAndInactiveUnitsOrdered()
    {
        var tenantId = Guid.NewGuid();
        var condominioId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var dbContext = CreateSqliteDbContext(tenantId);
        await SeedRequiredForeignKeysAsync(dbContext, condominioId, userId);
        var repository = new UnidadeRepository(dbContext);

        var bloco = CreateBloco(tenantId, condominioId, userId);
        var unidadeA = CreateUnidade(bloco: bloco, userId: userId, andar: 1, numero: "101A");
        var unidadeB = CreateUnidade(bloco: bloco, userId: userId, andar: 1, numero: "102");
        var unidadeC = CreateUnidade(bloco: bloco, userId: userId, andar: 2, numero: "201");
        unidadeA.Inativar(userId, new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        dbContext.Blocos.Add(bloco);
        await repository.AddAsync(unidadeC);
        await repository.AddAsync(unidadeA);
        await repository.AddAsync(unidadeB);
        await repository.SaveAsync();

        var unidades = await repository.ListByBlocoAsync(bloco.Id, includeInactive: true);

        Assert.Equal(3, unidades.Count);
        Assert.Equal(["101A", "102", "201"], unidades.Select(unidade => unidade.Numero).ToArray());
        Assert.Contains(unidades, unidade => !unidade.Ativo && unidade.Numero == "101A");
    }

    [Fact]
    public void UnidadeEvents_ShouldExposeExpectedEventTypes()
    {
        var occurredAt = new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero);

        var created = new UnidadeCriadaV1(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "101", Guid.NewGuid(), occurredAt);
        var inactivated = new UnidadeInativadaV1(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "101", Guid.NewGuid(), occurredAt);
        var reactivated = new UnidadeReativadaV1(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "101", Guid.NewGuid(), occurredAt);

        Assert.Equal("unidade.criada.v1", created.EventType);
        Assert.Equal("unidade.inativada.v1", inactivated.EventType);
        Assert.Equal("unidade.reativada.v1", reactivated.EventType);
    }

    private static Bloco CreateBloco(
        Guid? tenantId = null,
        Guid? condominioId = null,
        Guid? userId = null,
        string nome = "Bloco A")
    {
        return Bloco.Create(
            Guid.NewGuid(),
            tenantId ?? Guid.NewGuid(),
            condominioId ?? Guid.NewGuid(),
            nome,
            userId ?? Guid.NewGuid(),
            new FakeTimeProvider(new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero))).Value!;
    }

    private static Unidade CreateUnidade(
        Bloco? bloco = null,
        Guid? userId = null,
        int andar = 2,
        string numero = "201")
    {
        var blocoValue = bloco ?? CreateBloco();
        var actor = userId ?? Guid.NewGuid();

        return Unidade.Create(
            Guid.NewGuid(),
            blocoValue.TenantId,
            blocoValue,
            andar,
            numero,
            actor,
            new FakeTimeProvider(new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero))).Value!;
    }

    private static AppDbContext CreateSqliteDbContext(Guid tenantId)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        var tenantContext = new TenantContext();
        tenantContext.BeginScope(tenantId);

        var context = new AppDbContext(options, tenantContext);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task SeedRequiredForeignKeysAsync(
        AppDbContext dbContext,
        Guid condominioId,
        Guid userId)
    {
        dbContext.Users.Add(new AppUser
        {
            Id = userId,
            UserName = $"user-{userId:N}@portabox.test",
            NormalizedUserName = $"USER-{userId:N}@PORTABOX.TEST",
            Email = $"user-{userId:N}@portabox.test",
            NormalizedEmail = $"USER-{userId:N}@PORTABOX.TEST",
            EmailConfirmed = true
        });

        dbContext.Condominios.Add(Condominio.Create(
            condominioId,
            "Condominio Seed",
            "12.345.678/0001-95",
            userId,
            TimeProvider.System));

        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
