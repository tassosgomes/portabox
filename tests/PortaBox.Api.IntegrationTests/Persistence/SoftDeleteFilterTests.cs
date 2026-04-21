using Microsoft.EntityFrameworkCore;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Domain.Abstractions;
using PortaBox.Infrastructure.MultiTenancy;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Api.IntegrationTests.Persistence;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class SoftDeleteFilterTests(PostgresDatabaseFixture fixture)
{
    [Fact]
    public async Task DefaultQueries_ShouldReturnOnlyActiveEntitiesFromCurrentTenant()
    {
        await fixture.ResetAsync();
        await EnsureTestTableAsync();

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using (var seedContext = CreateDbContext())
        {
            seedContext.TestSoftDeletables.AddRange(
                TestSoftDeletableEntity.CreateActive(tenantA, "A-ativa"),
                TestSoftDeletableEntity.CreateInactive(tenantA, "A-inativa"),
                TestSoftDeletableEntity.CreateActive(tenantB, "B-ativa"));

            await seedContext.SaveChangesAsync();
        }

        await using (var tenantAContext = CreateDbContext(tenantA))
        {
            var results = await tenantAContext.TestSoftDeletables
                .OrderBy(entity => entity.Nome)
                .ToListAsync();

            Assert.Single(results);
            Assert.Equal("A-ativa", results[0].Nome);
            Assert.Equal(tenantA, results[0].TenantId);
            Assert.True(results[0].Ativo);
        }

        await using (var tenantBContext = CreateDbContext(tenantB))
        {
            var results = await tenantBContext.TestSoftDeletables
                .OrderBy(entity => entity.Nome)
                .ToListAsync();

            Assert.Single(results);
            Assert.Equal("B-ativa", results[0].Nome);
            Assert.Equal(tenantB, results[0].TenantId);
        }
    }

    [Fact]
    public async Task IgnoreQueryFilters_ShouldReturnActiveAndInactiveEntitiesAcrossTenants()
    {
        await fixture.ResetAsync();
        await EnsureTestTableAsync();

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using (var seedContext = CreateDbContext())
        {
            seedContext.TestSoftDeletables.AddRange(
                TestSoftDeletableEntity.CreateActive(tenantA, "A-ativa"),
                TestSoftDeletableEntity.CreateInactive(tenantA, "A-inativa"),
                TestSoftDeletableEntity.CreateActive(tenantB, "B-ativa"));

            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext(tenantA);

        var results = await context.TestSoftDeletables
            .IgnoreQueryFilters()
            .OrderBy(entity => entity.Nome)
            .ToListAsync();

        Assert.Equal(3, results.Count);
        Assert.Equal(["A-ativa", "A-inativa", "B-ativa"], results.Select(entity => entity.Nome).ToArray());
    }

    private TestSoftDeleteDbContext CreateDbContext(Guid? tenantId = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        return new TestSoftDeleteDbContext(options, CreateTenantContext(tenantId));
    }

    private static ITenantContext CreateTenantContext(Guid? tenantId)
    {
        var tenantContext = new TenantContext();

        if (tenantId is { } value)
        {
            tenantContext.BeginScope(value);
        }

        return tenantContext;
    }

    private async Task EnsureTestTableAsync()
    {
        await using var connection = await fixture.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS public.test_soft_deletable (
                id uuid PRIMARY KEY,
                tenant_id uuid NOT NULL,
                nome character varying(100) NOT NULL,
                ativo boolean NOT NULL,
                inativado_em timestamp with time zone NULL,
                inativado_por uuid NULL
            );
            """;

        await command.ExecuteNonQueryAsync();
    }

    private sealed class TestSoftDeleteDbContext(
        DbContextOptions<AppDbContext> options,
        ITenantContext tenantContext)
        : AppDbContext(options, tenantContext)
    {
        public DbSet<TestSoftDeletableEntity> TestSoftDeletables => Set<TestSoftDeletableEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestSoftDeletableEntity>();
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TestSoftDeletableEntity>(builder =>
            {
                builder.ToTable("test_soft_deletable");
                builder.HasKey(entity => entity.Id);
                builder.Property(entity => entity.Id).ValueGeneratedNever();
                builder.Property(entity => entity.TenantId).IsRequired();
                builder.Property(entity => entity.Nome).HasMaxLength(100).IsRequired();
                builder.Property(entity => entity.Ativo).IsRequired();
            });
        }
    }

    private sealed class TestSoftDeletableEntity : ITenantEntity, ISoftDeletable
    {
        public Guid Id { get; private set; }

        public Guid TenantId { get; private set; }

        public string Nome { get; private set; } = string.Empty;

        public bool Ativo { get; private set; }

        public DateTime? InativadoEm { get; private set; }

        public Guid? InativadoPor { get; private set; }

        public static TestSoftDeletableEntity CreateActive(Guid tenantId, string nome)
        {
            return new TestSoftDeletableEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Nome = nome,
                Ativo = true
            };
        }

        public static TestSoftDeletableEntity CreateInactive(Guid tenantId, string nome)
        {
            return new TestSoftDeletableEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Nome = nome,
                Ativo = false,
                InativadoEm = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc),
                InativadoPor = Guid.NewGuid()
            };
        }
    }
}
