using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Domain.Abstractions;
using PortaBox.Infrastructure.Email;
using PortaBox.Infrastructure.Events;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.MagicLinks;
using PortaBox.Modules.Gestao.Domain.Blocos;
using PortaBox.Modules.Gestao.Domain;
using PortaBox.Modules.Gestao.Domain.Unidades;

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

    public DbSet<Bloco> Blocos => Set<Bloco>();

    public DbSet<Unidade> Unidades => Set<Unidade>();

    public DbSet<EmailOutboxEntry> EmailOutboxEntries => Set<EmailOutboxEntry>();

    public DbSet<DomainEventOutboxEntry> DomainEventOutboxEntries => Set<DomainEventOutboxEntry>();

    public DbSet<MagicLink> MagicLinks => Set<MagicLink>();

    public DbSet<OptInDocument> OptInDocuments => Set<OptInDocument>();

    public DbSet<OptInRecord> OptInRecords => Set<OptInRecord>();

    public DbSet<Sindico> Sindicos => Set<Sindico>();

    public DbSet<TenantAuditEntry> TenantAuditEntries => Set<TenantAuditEntry>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        ConfigureIdentityTables(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Apply one combined global query filter per entity so tenant isolation and soft-delete
        // compose without EF Core overriding a previous HasQueryFilter call.
        ApplyGlobalQueryFilters(modelBuilder);
    }

    private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        var tenantEntityInterface = typeof(ITenantEntity);
        var softDeletableInterface = typeof(ISoftDeletable);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var isTenantEntity = tenantEntityInterface.IsAssignableFrom(clrType);
            var isSoftDeletable = softDeletableInterface.IsAssignableFrom(clrType);

            if (!isTenantEntity && !isSoftDeletable)
            {
                continue;
            }

            var method = typeof(AppDbContext)
                .GetMethod(nameof(ApplyGlobalFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(clrType);

            method.Invoke(this, [modelBuilder]);
        }
    }

    private void ApplyGlobalFilter<T>(ModelBuilder modelBuilder)
        where T : class
    {
        Expression? body = null;
        var parameter = Expression.Parameter(typeof(T), "e");

        if (typeof(ITenantEntity).IsAssignableFrom(typeof(T)))
        {
            var tenantProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
            var tenantId = Expression.Property(Expression.Constant(this), nameof(CurrentTenantId));
            body = Expression.Equal(Expression.Convert(tenantProperty, typeof(Guid?)), tenantId);
        }

        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
        {
            var activeProperty = Expression.Property(parameter, nameof(ISoftDeletable.Ativo));
            var activeFilter = Expression.Equal(activeProperty, Expression.Constant(true));
            body = body is null ? activeFilter : Expression.AndAlso(body, activeFilter);
        }

        if (body is null)
        {
            return;
        }

        modelBuilder.Entity<T>().HasQueryFilter(Expression.Lambda<Func<T, bool>>(body, parameter));
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
