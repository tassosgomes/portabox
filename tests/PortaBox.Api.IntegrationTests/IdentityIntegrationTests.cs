using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Api.IntegrationTests;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class IdentityIntegrationTests(PostgresDatabaseFixture fixture)
{
    [Fact]
    public async Task IdentityMigration_ShouldApplyWithoutErrors_OnFreshDatabase()
    {
        var databaseName = $"portabox_identity_{Guid.NewGuid():N}";
        var adminConnectionString = BuildConnectionString("postgres");
        var testDatabaseConnectionString = BuildConnectionString(databaseName);

        await using var adminConnection = new NpgsqlConnection(adminConnectionString);
        await adminConnection.OpenAsync();

        await using (var createDatabaseCommand = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\";", adminConnection))
        {
            await createDatabaseCommand.ExecuteNonQueryAsync();
        }

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(testDatabaseConnectionString)
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
                .Options;

            await using var dbContext = new AppDbContext(options);
            await dbContext.Database.MigrateAsync();

            await using var verificationConnection = new NpgsqlConnection(testDatabaseConnectionString);
            await verificationConnection.OpenAsync();

            await using var verificationCommand = new NpgsqlCommand(
                """
                SELECT COUNT(*)
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_name IN ('asp_net_users', 'asp_net_roles', 'asp_net_user_roles');
                """,
                verificationConnection);

            var tableCount = (long)(await verificationCommand.ExecuteScalarAsync() ?? 0L);
            Assert.Equal(3L, tableCount);
        }
        finally
        {
            await using (var revokeConnectCommand = new NpgsqlCommand(
                $"""REVOKE CONNECT ON DATABASE "{databaseName}" FROM PUBLIC;""",
                adminConnection))
            {
                await revokeConnectCommand.ExecuteNonQueryAsync();
            }

            await using (var terminateConnectionsCommand = new NpgsqlCommand(
                $"""
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '{databaseName}';
                """,
                adminConnection))
            {
                await terminateConnectionsCommand.ExecuteNonQueryAsync();
            }

            await using var dropDatabaseCommand = new NpgsqlCommand(
                $"""DROP DATABASE "{databaseName}";""",
                adminConnection);
            await dropDatabaseCommand.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task Seeder_ShouldCreateOperatorAndSindicoRoles()
    {
        await fixture.ResetAsync();
        await using var testContext = await IntegrationIdentityContext.CreateAsync(fixture.ConnectionString, "Production");

        await testContext.Seeder.RunAsync();

        Assert.True(await testContext.RoleManager.RoleExistsAsync(IdentityRoles.Operator));
        Assert.True(await testContext.RoleManager.RoleExistsAsync(IdentityRoles.Sindico));
    }

    [Fact]
    public async Task Seeder_ShouldCreateDevelopmentOperatorUserWithOperatorRole_InDevelopment()
    {
        await fixture.ResetAsync();
        await using var testContext = await IntegrationIdentityContext.CreateAsync(fixture.ConnectionString, "Development");

        await testContext.Seeder.RunAsync();

        var user = await testContext.UserManager.FindByEmailAsync("operator@portabox.dev");

        Assert.NotNull(user);
        Assert.True(await testContext.UserManager.IsInRoleAsync(user, IdentityRoles.Operator));
    }

    private string BuildConnectionString(string database)
    {
        var builder = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = database
        };

        return builder.ConnectionString;
    }

    private sealed class IntegrationIdentityContext : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly AsyncServiceScope _scope;

        private IntegrationIdentityContext(
            ServiceProvider serviceProvider,
            AsyncServiceScope scope,
            AppDbContext dbContext,
            IdentitySeeder seeder,
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager)
        {
            _serviceProvider = serviceProvider;
            _scope = scope;
            DbContext = dbContext;
            Seeder = seeder;
            UserManager = userManager;
            RoleManager = roleManager;
        }

        public AppDbContext DbContext { get; }

        public IdentitySeeder Seeder { get; }

        public UserManager<AppUser> UserManager { get; }

        public RoleManager<AppRole> RoleManager { get; }

        public static async Task<IntegrationIdentityContext> CreateAsync(string connectionString, string environmentName)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(environmentName));
            services.AddSingleton<IOptions<IdentityConfiguration>>(Microsoft.Extensions.Options.Options.Create(new IdentityConfiguration()));
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString)
                       .UseSnakeCaseNamingConvention()
                       .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));
            services
                .AddIdentityCore<AppUser>()
                .AddRoles<AppRole>()
                .AddEntityFrameworkStores<AppDbContext>();
            services.AddScoped<IdentitySeeder>();

            var serviceProvider = services.BuildServiceProvider();
            var scope = serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();

            return new IntegrationIdentityContext(
                serviceProvider,
                scope,
                dbContext,
                scope.ServiceProvider.GetRequiredService<IdentitySeeder>(),
                scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>(),
                scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>());
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _scope.DisposeAsync();
            await _serviceProvider.DisposeAsync();
        }
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "PortaBox.IntegrationTests";

        public string EnvironmentName { get; set; } = environmentName;

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
