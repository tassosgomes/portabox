using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PortaBox.Infrastructure;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao.Application.Repositories;

namespace PortaBox.Api.UnitTests;

public class AppDbContextAndInfrastructureRegistrationTests
{
    [Fact]
    public void AppDbContext_ShouldApplySnakeCaseConvention()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=portabox_unit;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        using var dbContext = new SnakeCaseProbeDbContext(options);

        var entityType = dbContext.Model.FindEntityType(typeof(SampleEntity));

        Assert.NotNull(entityType);
        Assert.Equal("sample_entity", entityType.GetTableName());

        var property = entityType.FindProperty(nameof(SampleEntity.SomeColumn));

        Assert.NotNull(property);
        Assert.Equal("some_column", property.GetColumnName(StoreObjectIdentifier.Table("sample_entity", null)));
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterScopedDbContextAndResolveIt()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=localhost;Port=5432;Database=portabox_di;Username=postgres;Password=postgres"
            })
            .Build();

        services.AddInfrastructure(configuration);

        var dbContextDescriptor = services.Single(service => service.ServiceType == typeof(AppDbContext));
        Assert.Equal(ServiceLifetime.Scoped, dbContextDescriptor.Lifetime);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<AppDbContext>>();
        var dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();

        Assert.NotNull(dbContext);
        Assert.NotNull(options);
        Assert.NotNull(dataSource);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IOptInRecordRepository>());
    }

    [Fact]
    public void AddInfrastructure_ShouldThrowWhenConnectionStringIsMissing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var act = () => services.AddInfrastructure(configuration);

        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Equal("Connection string 'Postgres' was not configured.", exception.Message);
    }

    [Fact]
    public void AddPortaBoxInfrastructure_ShouldResolveDbContextUsingAliasMethod()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=localhost;Port=5432;Database=portabox_alias;Username=postgres;Password=postgres"
            })
            .Build();

        services.AddPortaBoxInfrastructure(configuration);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.NotNull(dbContext);
    }

    [Fact]
    public void AppDbContextFactory_ShouldPreferEnvironmentVariableConnectionString()
    {
        const string connectionString = "Host=localhost;Port=5432;Database=portabox_factory_env;Username=postgres;Password=postgres";
        var previousValue = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");

        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", connectionString);

            var factory = new AppDbContextFactory();
            using var dbContext = factory.CreateDbContext([]);

            var resolvedConnectionString = dbContext.Database.GetConnectionString();

            Assert.Contains("Host=localhost", resolvedConnectionString, StringComparison.Ordinal);
            Assert.Contains("Database=portabox_factory_env", resolvedConnectionString, StringComparison.Ordinal);
            Assert.Contains("Username=postgres", resolvedConnectionString, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", previousValue);
        }
    }

    [Fact]
    public void AppDbContextFactory_ShouldFallbackToDefaultConnectionString()
    {
        var previousValue = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");

        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", null);

            var factory = new AppDbContextFactory();
            using var dbContext = factory.CreateDbContext([]);

            var resolvedConnectionString = dbContext.Database.GetConnectionString();

            Assert.Contains("Host=localhost", resolvedConnectionString, StringComparison.Ordinal);
            Assert.Contains("Database=portabox", resolvedConnectionString, StringComparison.Ordinal);
            Assert.Contains("Username=postgres", resolvedConnectionString, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", previousValue);
        }
    }

    private sealed class SnakeCaseProbeDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SampleEntity>();
        }
    }

    private sealed class SampleEntity
    {
        public int Id { get; set; }

        public string SomeColumn { get; set; } = string.Empty;
    }
}
