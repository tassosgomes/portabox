using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Infrastructure.Audit;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao.Application.Audit;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.UnitTests.Audit;

public class AuditServiceTests
{
    [Fact]
    public async Task RecordStructuralAsync_ShouldAddAuditEntryWithoutCommit()
    {
        var tenantId = Guid.NewGuid();
        var performedByUserId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero);
        var metadata = StructuralAuditMetadata.ForUnidadeCriada(Guid.NewGuid(), Guid.NewGuid(), 2, "201");

        await using var dbContext = CreateDbContext();
        var sut = new AuditService(dbContext, new FixedTimeProvider(occurredAt));

        await sut.RecordStructuralAsync(
            TenantAuditEventKind.UnidadeCriada,
            tenantId,
            performedByUserId,
            metadata,
            "Unidade criada",
            CancellationToken.None);

        Assert.Equal(0, dbContext.SaveChangesAsyncCallCount);

        var entry = dbContext.ChangeTracker.Entries<TenantAuditEntry>().Single();
        Assert.Equal(EntityState.Added, entry.State);
        Assert.Equal(TenantAuditEventKind.UnidadeCriada, entry.Entity.EventKind);
        Assert.Equal(tenantId, entry.Entity.TenantId);
        Assert.Equal(performedByUserId, entry.Entity.PerformedByUserId);
        Assert.Equal(occurredAt, entry.Entity.OccurredAt);
        Assert.Equal("Unidade criada", entry.Entity.Note);

        using var document = JsonDocument.Parse(entry.Entity.MetadataJson!);
        Assert.Equal(metadata["unidadeId"].ToString(), document.RootElement.GetProperty("unidadeId").GetString());
        Assert.Equal(metadata["blocoId"].ToString(), document.RootElement.GetProperty("blocoId").GetString());
        Assert.Equal((int)metadata["andar"], document.RootElement.GetProperty("andar").GetInt32());
        Assert.Equal((string)metadata["numero"], document.RootElement.GetProperty("numero").GetString());
    }

    private static SpyAppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"audit-service-tests-{Guid.NewGuid()}")
            .Options;

        return new SpyAppDbContext(options, new FakeTenantContext());
    }

    private sealed class SpyAppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
        : AppDbContext(options, tenantContext)
    {
        public int SaveChangesAsyncCallCount { get; private set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesAsyncCallCount++;
            return base.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeTenantContext : ITenantContext
    {
        public Guid? TenantId => null;

        public IDisposable BeginScope(Guid tenantId) => new Scope();

        private sealed class Scope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
