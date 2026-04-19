using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Domain.Abstractions;
using PortaBox.Infrastructure.Email;
using PortaBox.Infrastructure.Events;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.MagicLinks;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the PortaBox application.
/// </summary>
/// <remarks>
/// Multi-tenancy rule: every entity type that implements <see cref="ITenantEntity" /> receives an
/// automatic global query filter <c>e.TenantId == _tenantContext.TenantId</c> applied in
/// <see cref="OnModelCreating" /> via reflection. To bypass the filter for audited operations
/// (e.g. backoffice cross-tenant queries) call <c>dbContext.Set&lt;T&gt;().IgnoreQueryFilters()</c>
/// explicitly and only in paths that require it.
///
/// Entities that are intentionally global (Condominio, AspNet*, audit tables, outbox) MUST NOT
/// implement <see cref="ITenantEntity" /> — they will not receive any filter.
/// </remarks>
public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ITenantContext? tenantContext = null)
    : IdentityDbContext<
        AppUser,
        AppRole,
        Guid,
        IdentityUserClaim<Guid>,
        IdentityUserRole<Guid>,
        IdentityUserLogin<Guid>,
        IdentityRoleClaim<Guid>,
        IdentityUserToken<Guid>>(options)
{
    // Null-coalescing fallback lets the design-time factory (AppDbContextFactory) create a context
    // without needing to wire up a real ITenantContext — migrations work correctly in that case.
    private readonly ITenantContext _tenantContext = tenantContext ?? NullTenantContext.Instance;

    internal Guid? CurrentTenantId => _tenantContext.TenantId;

    public DbSet<Condominio> Condominios => Set<Condominio>();

    public DbSet<EmailOutboxEntry> EmailOutboxEntries => Set<EmailOutboxEntry>();

    public DbSet<DomainEventOutboxEntry> DomainEventOutboxEntries => Set<DomainEventOutboxEntry>();

    public DbSet<MagicLink> MagicLinks => Set<MagicLink>();

    public DbSet<OptInDocument> OptInDocuments => Set<OptInDocument>();

    public DbSet<OptInRecord> OptInRecords => Set<OptInRecord>();

    public DbSet<Sindico> Sindicos => Set<Sindico>();

    public DbSet<TenantAuditEntry> TenantAuditEntries => Set<TenantAuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        ConfigureIdentityTables(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Apply a global query filter for every entity type that implements ITenantEntity.
        // This ensures that no query leaks data across tenants unless IgnoreQueryFilters() is
        // called explicitly (reserved for audited, cross-tenant backoffice operations).
        ApplyTenantQueryFilters(modelBuilder);
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        var tenantEntityInterface = typeof(ITenantEntity);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!tenantEntityInterface.IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            // Build the lambda: (ITenantEntity e) => e.TenantId == _tenantContext.TenantId
            // via the generic helper to satisfy EF Core's typed filter requirement.
            var method = typeof(AppDbContext)
                .GetMethod(nameof(ApplyTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(this, [modelBuilder]);
        }
    }

    private void ApplyTenantFilter<T>(ModelBuilder modelBuilder)
        where T : class, ITenantEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
    }

    private static void ConfigureIdentityTables(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("asp_net_users");
            entity.HasIndex(user => user.NormalizedUserName).HasDatabaseName("ix_asp_net_users_normalized_user_name").IsUnique();
            entity.HasIndex(user => user.NormalizedEmail).HasDatabaseName("ix_asp_net_users_normalized_email");
            entity.HasOne<Condominio>()
                .WithMany()
                .HasForeignKey(user => user.SindicoTenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppRole>(entity =>
        {
            entity.ToTable("asp_net_roles");
            entity.HasIndex(role => role.NormalizedName).HasDatabaseName("ix_asp_net_roles_normalized_name").IsUnique();
        });

        modelBuilder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("asp_net_user_claims");
        });

        modelBuilder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("asp_net_user_roles");
        });

        modelBuilder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("asp_net_user_logins");
        });

        modelBuilder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("asp_net_role_claims");
        });

        modelBuilder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("asp_net_user_tokens");
        });
    }

    /// <summary>
    /// Fallback implementation used when no <see cref="ITenantContext" /> is injected (design-time
    /// context factory during migrations).
    /// </summary>
    private sealed class NullTenantContext : ITenantContext
    {
        public static readonly NullTenantContext Instance = new();

        private NullTenantContext() { }

        public Guid? TenantId => null;

        public IDisposable BeginScope(Guid tenantId) => NullScope.Instance;

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            private NullScope() { }
            public void Dispose() { }
        }
    }
}
