using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Domain.Abstractions;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Api.UnitTests;

public sealed class AppDbContextSoftDeleteQueryFilterTests
{
    [Fact]
    public void Model_ShouldApplyCombinedTenantAndSoftDeleteFilter_ToSoftDeletableTenantEntities()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=portabox_unit_filters;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        using var dbContext = new SoftDeleteProbeDbContext(options, new StubTenantContext(Guid.NewGuid()));

        var entityType = dbContext.Model.FindEntityType(typeof(TestSoftDeletableEntity));

        Assert.NotNull(entityType);

        var filter = entityType!.GetQueryFilter();

        Assert.NotNull(filter);
        Assert.Contains(nameof(ITenantEntity.TenantId), filter!.ToString(), StringComparison.Ordinal);
        Assert.Contains(nameof(ISoftDeletable.Ativo), filter.ToString(), StringComparison.Ordinal);
    }

    private sealed class SoftDeleteProbeDbContext(
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
                builder.ToTable("test_soft_deletable_unit");
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
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public string Nome { get; set; } = string.Empty;

        public bool Ativo { get; set; }

        public DateTime? InativadoEm { get; set; }

        public Guid? InativadoPor { get; set; }
    }

    private sealed class StubTenantContext(Guid? tenantId) : ITenantContext
    {
        public Guid? TenantId { get; } = tenantId;

        public IDisposable BeginScope(Guid tenantId) => throw new NotSupportedException();
    }
}
