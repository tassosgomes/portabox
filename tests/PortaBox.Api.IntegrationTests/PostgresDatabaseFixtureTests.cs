using Npgsql;
using PortaBox.Api.IntegrationTests.Fixtures;

namespace PortaBox.Api.IntegrationTests;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class PostgresDatabaseFixtureTests(PostgresDatabaseFixture fixture)
{
    [Fact]
    public async Task Fixture_ShouldStartPostgres16ApplyInitialCreateAndAnswerSelect1()
    {
        await using var connection = await fixture.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT version(), 1;", connection);
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());

        var version = reader.GetString(0);
        var result = reader.GetInt32(1);

        Assert.Contains("PostgreSQL 16", version, StringComparison.Ordinal);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ResetAsync_ShouldTruncateUserTablesWithoutDroppingSchema()
    {
        await fixture.ResetAsync();

        await using (var connection = await fixture.OpenConnectionAsync())
        {
            const string setupSql = """
                CREATE TABLE IF NOT EXISTS public.integration_reset_probe (
                    id integer PRIMARY KEY
                );

                INSERT INTO public.integration_reset_probe (id)
                VALUES (1)
                ON CONFLICT (id) DO NOTHING;
                """;

            await using var setup = new NpgsqlCommand(setupSql, connection);
            await setup.ExecuteNonQueryAsync();
        }

        await fixture.ResetAsync();

        await using var verificationConnection = await fixture.OpenConnectionAsync();
        await using var countCommand = new NpgsqlCommand(
            "SELECT COUNT(*) FROM public.integration_reset_probe;",
            verificationConnection);
        var rowCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

        await using var schemaCommand = new NpgsqlCommand(
            "SELECT to_regclass('public.integration_reset_probe') IS NOT NULL;",
            verificationConnection);
        var tableExists = (bool)(await schemaCommand.ExecuteScalarAsync())!;

        Assert.True(tableExists);
        Assert.Equal(0, rowCount);
    }

    [Fact]
    public async Task SharedFixtureTests_ShouldNotLeakStateAcrossSequentialRuns()
    {
        await fixture.ResetAsync();

        await using var firstConnection = await fixture.OpenConnectionAsync();
        await using (var createTable = new NpgsqlCommand(
            """
            CREATE TABLE IF NOT EXISTS public.integration_state_probe (
                id integer PRIMARY KEY
            );
            """,
            firstConnection))
        {
            await createTable.ExecuteNonQueryAsync();
        }

        await using (var insertCommand = new NpgsqlCommand(
            "INSERT INTO public.integration_state_probe (id) VALUES (10) ON CONFLICT (id) DO NOTHING;",
            firstConnection))
        {
            await insertCommand.ExecuteNonQueryAsync();
        }

        await fixture.ResetAsync();

        await using var secondConnection = await fixture.OpenConnectionAsync();
        await using var countCommand = new NpgsqlCommand(
            "SELECT COUNT(*) FROM public.integration_state_probe;",
            secondConnection);
        var rowCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

        Assert.Equal(0, rowCount);
    }
}
