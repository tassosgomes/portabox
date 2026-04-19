using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using PortaBox.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PortaBox.Api.IntegrationTests.Fixtures;

public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private const string DefaultDatabase = "portabox_integration";
    private const string DefaultUsername = "postgres";
    private const string DefaultPassword = "postgres";

    // Reuse avoids depending on a brand-new Resource Reaper startup for every test process.
    // This makes repeated runs of the critical tenant-isolation test stable while still letting
    // ResetAsync() guarantee a clean logical database state between tests.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase(DefaultDatabase)
        .WithUsername(DefaultUsername)
        .WithPassword(DefaultPassword)
        .WithReuse(true)
        .Build();

    private NpgsqlDataSource? _dataSource;
    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(ConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        _dataSource = dataSourceBuilder.Build();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_dataSource)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        await using var dbContext = new AppDbContext(options);
        await dbContext.Database.MigrateAsync();

        await using (var bootstrapConnection = await OpenConnectionAsync())
        {
            await using var bootstrapCommand = new NpgsqlCommand(
                """
                CREATE TABLE IF NOT EXISTS public.integration_respawn_seed (
                    id integer PRIMARY KEY
                );
                """,
                bootstrapConnection);
            await bootstrapCommand.ExecuteNonQueryAsync();
        }

    }

    public async Task DisposeAsync()
    {
        if (_dataSource is not null)
        {
            await _dataSource.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        return connection;
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var respawner = await DatabaseResetExtensions.CreateRespawnerAsync(ConnectionString, cancellationToken);
        await respawner.ResetDatabaseAsync(ConnectionString, cancellationToken);
    }
}
