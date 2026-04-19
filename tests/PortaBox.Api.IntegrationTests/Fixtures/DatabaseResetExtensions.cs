using Npgsql;
using Respawn;
using Respawn.Graph;

namespace PortaBox.Api.IntegrationTests.Fixtures;

public static class DatabaseResetExtensions
{
    public static async Task<Respawner> CreateRespawnerAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")]
        });
    }

    public static async Task ResetDatabaseAsync(
        this Respawner respawner,
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(respawner);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await respawner.ResetAsync(connection);
    }
}
