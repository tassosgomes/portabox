using Microsoft.EntityFrameworkCore;
using Npgsql;
using PortaBox.Domain.Abstractions;
using PortaBox.Infrastructure.MultiTenancy;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Api.IntegrationTests.Fixtures;

namespace PortaBox.Api.IntegrationTests.MultiTenancy;

/// <summary>
/// Integration tests that prove the EF Core global query filter isolates data per tenant.
///
/// These tests are the primary guard against cross-tenant data leakage. They run against a real
/// PostgreSQL instance via Testcontainers. The fixture (<see cref="PostgresDatabaseFixture" />) is
/// shared across tests in the collection — <see cref="fixture.ResetAsync" /> is called at the
/// start of each test to guarantee a clean state.
///
/// If any of these tests fail, it means the global query filter is broken or missing for a new
/// entity type. Fix the root cause in <c>AppDbContext.OnModelCreating</c> before merging.
/// </summary>
[Collection(nameof(PostgresDatabaseCollection))]
public sealed class TenantIsolationTests(PostgresDatabaseFixture fixture)
{
    // ─── DDL for test-only tables ─────────────────────────────────────────────

    private const string CreateTablesSql = """
        CREATE TABLE IF NOT EXISTS public.sample_tenant_entities (
            id          uuid         PRIMARY KEY,
            tenant_id   uuid         NOT NULL,
            label       varchar(200) NOT NULL
        );

        CREATE TABLE IF NOT EXISTS public.sample_global_entities (
            id   uuid         PRIMARY KEY,
            name varchar(200) NOT NULL
        );
        """;

    private const string TruncateSampleTablesSql = """
        TRUNCATE TABLE public.sample_tenant_entities;
        TRUNCATE TABLE public.sample_global_entities;
        """;

    // ─── critical isolation test ──────────────────────────────────────────────

    [Fact]
    public async Task QueryFilter_ShouldReturnOnlyEntitiesMatchingCurrentTenant()
    {
        // Arrange
        await EnsureTestTablesExistAsync();
        await TruncateSampleTablesAsync();

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await SeedSampleEntitiesAsync(tenantA, tenantB);

        // Act — query as tenant A with the EF Core global query filter active
        var tenantContext = new TenantContext();
        using var scopeA = tenantContext.BeginScope(tenantA);
        await using var ctxA = BuildContext(tenantContext);

        var resultsA = await ctxA.SampleTenantEntities.ToListAsync();

        // Assert — only tenant A's entity is visible
        Assert.Single(resultsA);
        Assert.Equal(tenantA, resultsA[0].TenantId);
    }

    [Fact]
    public async Task BeginScope_ShouldSwitchVisibility_WhenTenantChangesWithinSameRequest()
    {
        // Arrange
        await EnsureTestTablesExistAsync();
        await TruncateSampleTablesAsync();

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await SeedSampleEntitiesAsync(tenantA, tenantB);

        var tenantContext = new TenantContext();

        // Act — outer scope = tenant A
        using (tenantContext.BeginScope(tenantA))
        {
            await using var ctxA = BuildContext(tenantContext);
            var resultsA = await ctxA.SampleTenantEntities.ToListAsync();

            Assert.Single(resultsA);
            Assert.Equal(tenantA, resultsA[0].TenantId);

            // Inner scope = tenant B
            using (tenantContext.BeginScope(tenantB))
            {
                // A new DbContext instance is needed because EF Core evaluates the filter
                // expression against the context's captured ITenantContext at query time.
                await using var ctxB = BuildContext(tenantContext);
                var resultsB = await ctxB.SampleTenantEntities.ToListAsync();

                Assert.Single(resultsB);
                Assert.Equal(tenantB, resultsB[0].TenantId);
            }

            // After inner scope disposed → back to tenant A visibility
            await using var ctxA2 = BuildContext(tenantContext);
            var resultsA2 = await ctxA2.SampleTenantEntities.ToListAsync();

            Assert.Single(resultsA2);
            Assert.Equal(tenantA, resultsA2[0].TenantId);
        }
    }

    [Fact]
    public async Task IgnoreQueryFilters_ShouldReturnAllTenants_WhenCalledExplicitly()
    {
        // Arrange
        await EnsureTestTablesExistAsync();
        await TruncateSampleTablesAsync();

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await SeedSampleEntitiesAsync(tenantA, tenantB);

        var tenantContext = new TenantContext();

        // Act — use tenant A scope but explicitly bypass the filter (backoffice cross-tenant path)
        using (tenantContext.BeginScope(tenantA))
        {
            await using var ctx = BuildContext(tenantContext);
            var allResults = await ctx.SampleTenantEntities.IgnoreQueryFilters().ToListAsync();

            // Both tenant A and tenant B entities must be visible
            Assert.Equal(2, allResults.Count);
        }
    }

    [Fact]
    public async Task GlobalEntity_ShouldNotReceiveQueryFilter_WhenNotImplementingITenantEntity()
    {
        // Arrange
        await EnsureTestTablesExistAsync();
        await TruncateSampleTablesAsync();

        var tenantContext = new TenantContext();
        var tenantA = Guid.NewGuid();

        // Act — even when a tenant scope is active, global entities must be visible without needing
        // IgnoreQueryFilters().
        using (tenantContext.BeginScope(tenantA))
        {
            await using var ctx = BuildContext(tenantContext);

            ctx.SampleGlobalEntities.Add(new SampleGlobalEntity { Name = "global-entity" });
            await ctx.SaveChangesAsync();

            // Query without IgnoreQueryFilters — global entity must appear
            var results = await ctx.SampleGlobalEntities.ToListAsync();
            Assert.Single(results);
            Assert.Equal("global-entity", results[0].Name);
        }
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private TenantAwareTestDbContext BuildContext(TenantContext? tenantContext = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new TenantAwareTestDbContext(options, tenantContext);
    }

    private async Task EnsureTestTablesExistAsync()
    {
        await using var conn = await fixture.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(CreateTablesSql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task TruncateSampleTablesAsync()
    {
        await using var conn = await fixture.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(TruncateSampleTablesSql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Seeds two sample tenant entities using raw SQL to bypass EF query filters.</summary>
    private async Task SeedSampleEntitiesAsync(Guid tenantA, Guid tenantB)
    {
        const string sql = """
            INSERT INTO public.sample_tenant_entities (id, tenant_id, label)
            VALUES (@idA, @tenantA, 'Entity-A'),
                   (@idB, @tenantB, 'Entity-B');
            """;

        await using var conn = await fixture.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@idA", Guid.NewGuid());
        cmd.Parameters.AddWithValue("@tenantA", tenantA);
        cmd.Parameters.AddWithValue("@idB", Guid.NewGuid());
        cmd.Parameters.AddWithValue("@tenantB", tenantB);
        await cmd.ExecuteNonQueryAsync();
    }
}

// ─── test-internal entities ───────────────────────────────────────────────────

/// <summary>
/// Test-only entity that implements <see cref="ITenantEntity" />.  Used exclusively inside the
/// integration test suite to validate that the global query filter mechanism isolates data
/// correctly.  Must NOT be referenced from production entity graphs.
/// </summary>
internal sealed class SampleTenantEntity : ITenantEntity
{
    public Guid Id { get; init; }

    /// <inheritdoc />
    public Guid TenantId { get; init; }

    public string Label { get; init; } = string.Empty;
}

/// <summary>
/// Test-only entity that does NOT implement <see cref="ITenantEntity" />, representing a global
/// (cross-tenant) entity.  Used to verify that the query filter is not applied to global entities.
/// </summary>
internal sealed class SampleGlobalEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
}

// ─── test DbContext ───────────────────────────────────────────────────────────

/// <summary>
/// Extends <see cref="AppDbContext" /> with the two test-only entity sets defined above.
/// Inherits the multi-tenancy query filter applied by the base class.
/// </summary>
internal sealed class TenantAwareTestDbContext : AppDbContext
{
    public TenantAwareTestDbContext(DbContextOptions<AppDbContext> options, TenantContext? tenantContext)
        : base(options, tenantContext)
    {
    }

    public DbSet<SampleTenantEntity> SampleTenantEntities => Set<SampleTenantEntity>();
    public DbSet<SampleGlobalEntity> SampleGlobalEntities => Set<SampleGlobalEntity>();

    protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
    {
        // Register test entities BEFORE calling base so that base.ApplyTenantQueryFilters
        // can discover SampleTenantEntity (which implements ITenantEntity) and apply the filter.
        modelBuilder.Entity<SampleTenantEntity>(entity =>
        {
            entity.ToTable("sample_tenant_entities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Label);
        });

        modelBuilder.Entity<SampleGlobalEntity>(entity =>
        {
            entity.ToTable("sample_global_entities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name);
        });

        // Call base AFTER test entity registration so ApplyTenantQueryFilters sees them.
        base.OnModelCreating(modelBuilder);
    }
}
