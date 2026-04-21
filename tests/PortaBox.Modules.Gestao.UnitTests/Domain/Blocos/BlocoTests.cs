using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortaBox.Infrastructure.MultiTenancy;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Infrastructure.Repositories;
using PortaBox.Infrastructure.Identity;
using PortaBox.Modules.Gestao.Domain.Blocos;
using PortaBox.Modules.Gestao.Domain.Blocos.Events;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.UnitTests.Domain.Blocos;

public sealed class BlocoTests
{
    [Fact]
    public void Create_WithValidName_ShouldReturnSuccessAndRaiseCreatedEvent()
    {
        var now = new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero);

        var result = Bloco.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Bloco A",
            Guid.NewGuid(),
            new FakeTimeProvider(now));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Ativo);
        Assert.Equal("Bloco A", result.Value.Nome);

        var domainEvent = Assert.IsType<BlocoCriadoV1>(Assert.Single(result.Value.DomainEvents));
        Assert.Equal(result.Value.Id, domainEvent.BlocoId);
        Assert.Equal(result.Value.TenantId, domainEvent.TenantId);
        Assert.Equal(result.Value.CriadoEm, domainEvent.OccurredAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123456789012345678901234567890123456789012345678901")]
    public void Create_WithInvalidName_ShouldReturnFailure(string nome)
    {
        var result = Bloco.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            nome,
            Guid.NewGuid(),
            TimeProvider.System);

        Assert.False(result.IsSuccess);
        Assert.Equal("O nome do bloco deve ter entre 1 e 50 caracteres.", result.Error);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Create_ShouldTrimNameBeforeStoring()
    {
        var result = Bloco.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "  Bloco Unico  ",
            Guid.NewGuid(),
            TimeProvider.System);

        Assert.True(result.IsSuccess);
        Assert.Equal("Bloco Unico", result.Value!.Nome);
    }

    [Fact]
    public void Rename_WithValidName_ShouldUpdateNameAndRaiseEvent()
    {
        var bloco = CreateBloco();

        var result = bloco.Rename("Torre Alfa", Guid.NewGuid(), new DateTime(2026, 4, 20, 13, 0, 0, DateTimeKind.Utc));

        Assert.True(result.IsSuccess);
        Assert.Equal("Torre Alfa", bloco.Nome);

        var domainEvent = Assert.IsType<BlocoRenomeadoV1>(bloco.DomainEvents.Last());
        Assert.Equal("Bloco A", domainEvent.NomeAntes);
        Assert.Equal("Torre Alfa", domainEvent.NomeDepois);
    }

    [Fact]
    public void Rename_WhenBlocoIsInactive_ShouldReturnFailure()
    {
        var bloco = CreateBloco();
        bloco.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 12, 30, 0, DateTimeKind.Utc));

        var result = bloco.Rename("Torre Alfa", Guid.NewGuid(), new DateTime(2026, 4, 20, 13, 0, 0, DateTimeKind.Utc));

        Assert.False(result.IsSuccess);
        Assert.Equal("Nao e possivel renomear bloco inativo.", result.Error);
    }

    [Fact]
    public void Rename_WithCurrentName_ShouldReturnFailure()
    {
        var bloco = CreateBloco();

        var result = bloco.Rename("Bloco A", Guid.NewGuid(), new DateTime(2026, 4, 20, 13, 0, 0, DateTimeKind.Utc));

        Assert.False(result.IsSuccess);
        Assert.Equal("O novo nome do bloco deve ser diferente do nome atual.", result.Error);
    }

    [Fact]
    public void Inativar_ShouldRaiseInactivatedEvent()
    {
        var bloco = CreateBloco();

        var result = bloco.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        Assert.True(result.IsSuccess);
        var domainEvent = Assert.IsType<BlocoInativadoV1>(bloco.DomainEvents.Last());
        Assert.Equal(bloco.Id, domainEvent.BlocoId);
        Assert.Equal(bloco.Nome, domainEvent.Nome);
    }

    [Fact]
    public void Reativar_ShouldRaiseReactivatedEvent()
    {
        var bloco = CreateBloco();
        bloco.Inativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        var result = bloco.Reativar(Guid.NewGuid(), new DateTime(2026, 4, 20, 15, 0, 0, DateTimeKind.Utc));

        Assert.True(result.IsSuccess);
        var domainEvent = Assert.IsType<BlocoReativadoV1>(bloco.DomainEvents.Last());
        Assert.Equal(bloco.Id, domainEvent.BlocoId);
        Assert.Equal(bloco.Nome, domainEvent.Nome);
    }

    [Fact]
    public async Task ExistsActiveWithNameAsync_ShouldIgnoreInactiveBlocoWithSameName()
    {
        var tenantId = Guid.NewGuid();
        var condominioId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var dbContext = CreateDbContext(tenantId);
        var repository = new BlocoRepository(dbContext);
        var bloco = CreateBloco(tenantId, condominioId, userId);
        bloco.Inativar(userId, new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        await repository.AddAsync(bloco);
        await repository.SaveAsync();

        var exists = await repository.ExistsActiveWithNameAsync(condominioId, bloco.Nome);

        Assert.False(exists);
    }

    [Fact]
    public async Task ListByCondominioAsync_WithIncludeInactiveTrue_ShouldReturnActiveAndInactiveBlocos()
    {
        var tenantId = Guid.NewGuid();
        var condominioId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var dbContext = CreateSqliteDbContext(tenantId);
        var repository = new BlocoRepository(dbContext);
        await SeedRequiredForeignKeysAsync(dbContext, condominioId, userId);

        var blocoAtivo = CreateBloco(tenantId, condominioId, userId);
        var blocoInativo = CreateBloco(tenantId, condominioId, userId, "Bloco B");
        blocoInativo.Inativar(userId, new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        await repository.AddAsync(blocoAtivo);
        await repository.AddAsync(blocoInativo);
        await repository.SaveAsync();

        var blocos = await repository.ListByCondominioAsync(condominioId, includeInactive: true);

        Assert.Equal(2, blocos.Count);
        Assert.Equal(["Bloco A", "Bloco B"], blocos.Select(bloco => bloco.Nome).ToArray());
    }

    [Fact]
    public async Task GetByIdIncludingInactiveAsync_ShouldReturnInactiveBloco()
    {
        var tenantId = Guid.NewGuid();
        var condominioId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var dbContext = CreateSqliteDbContext(tenantId);
        var repository = new BlocoRepository(dbContext);
        await SeedRequiredForeignKeysAsync(dbContext, condominioId, userId);
        var bloco = CreateBloco(tenantId, condominioId, userId);
        bloco.Inativar(userId, new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));

        await repository.AddAsync(bloco);
        await repository.SaveAsync();

        var loaded = await repository.GetByIdIncludingInactiveAsync(bloco.Id);

        Assert.NotNull(loaded);
        Assert.Equal(bloco.Id, loaded.Id);
        Assert.False(loaded.Ativo);
    }

    private static Bloco CreateBloco(
        Guid? tenantId = null,
        Guid? condominioId = null,
        Guid? userId = null,
        string nome = "Bloco A")
    {
        var result = Bloco.Create(
            Guid.NewGuid(),
            tenantId ?? Guid.NewGuid(),
            condominioId ?? Guid.NewGuid(),
            nome,
            userId ?? Guid.NewGuid(),
            new FakeTimeProvider(new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero)));

        return result.Value!;
    }

    private static AppDbContext CreateDbContext(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseSnakeCaseNamingConvention()
            .Options;

        var tenantContext = new TenantContext();
        tenantContext.BeginScope(tenantId);

        return new AppDbContext(options, tenantContext);
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
