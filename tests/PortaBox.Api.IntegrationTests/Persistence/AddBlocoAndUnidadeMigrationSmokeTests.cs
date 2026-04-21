using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Api.IntegrationTests.Persistence;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class AddBlocoAndUnidadeMigrationSmokeTests(PostgresDatabaseFixture fixture)
{
    [Fact]
    public async Task Up_OnEmptyDatabase_ShouldCreateBlocoAndUnidadeWithExpectedColumns()
    {
        await fixture.ResetAsync();

        await using var dbContext = CreateDbContext();
        var migrator = dbContext.Database.GetService<IMigrator>();
        var migrationIds = dbContext.Database.GetMigrations().ToArray();
        var latestMigration = Assert.Single(migrationIds, id => id.EndsWith("_AddBlocoAndUnidade", StringComparison.Ordinal));

        await migrator.MigrateAsync("0");
        await migrator.MigrateAsync(latestMigration);

        var blocoColumns = await GetColumnDefinitionsAsync("bloco");
        Assert.Contains(blocoColumns, column => column is ("tenant_id", "uuid"));
        Assert.Contains(blocoColumns, column => column is ("condominio_id", "uuid"));
        Assert.Contains(blocoColumns, column => column is ("nome", "character varying"));
        Assert.Contains(blocoColumns, column => column is ("ativo", "boolean"));
        Assert.Contains(blocoColumns, column => column is ("criado_por", "uuid"));
        Assert.Contains(blocoColumns, column => column is ("inativado_por", "uuid"));

        var unidadeColumns = await GetColumnDefinitionsAsync("unidade");
        Assert.Contains(unidadeColumns, column => column is ("bloco_id", "uuid"));
        Assert.Contains(unidadeColumns, column => column is ("andar", "integer"));
        Assert.Contains(unidadeColumns, column => column is ("numero", "character varying"));
        Assert.Contains(unidadeColumns, column => column is ("ativo", "boolean"));
        Assert.Contains(unidadeColumns, column => column is ("criado_por", "uuid"));
        Assert.Contains(unidadeColumns, column => column is ("inativado_por", "uuid"));
    }

    [Fact]
    public async Task Up_ShouldCreatePartialUniqueIndexesForActiveRowsOnly()
    {
        await fixture.ResetAsync();

        await using var dbContext = CreateDbContext();
        var migrator = dbContext.Database.GetService<IMigrator>();
        var migrationIds = dbContext.Database.GetMigrations().ToArray();
        var latestMigration = Assert.Single(migrationIds, id => id.EndsWith("_AddBlocoAndUnidade", StringComparison.Ordinal));

        await migrator.MigrateAsync("0");
        await migrator.MigrateAsync(latestMigration);

        var blocoIndexDefinition = await GetIndexDefinitionAsync("idx_bloco_nome_ativo_unique");
        Assert.NotNull(blocoIndexDefinition);
        Assert.Contains("CREATE UNIQUE INDEX idx_bloco_nome_ativo_unique", blocoIndexDefinition, StringComparison.Ordinal);
        Assert.Contains("WHERE", blocoIndexDefinition, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ativo = true", blocoIndexDefinition, StringComparison.OrdinalIgnoreCase);

        var unidadeIndexDefinition = await GetIndexDefinitionAsync("idx_unidade_canonica_ativa");
        Assert.NotNull(unidadeIndexDefinition);
        Assert.Contains("CREATE UNIQUE INDEX idx_unidade_canonica_ativa", unidadeIndexDefinition, StringComparison.Ordinal);
        Assert.Contains("WHERE", unidadeIndexDefinition, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ativo = true", unidadeIndexDefinition, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Down_ShouldRemoveBlocoAndUnidadeTablesAndIndexes()
    {
        await fixture.ResetAsync();

        await using var dbContext = CreateDbContext();
        var migrator = dbContext.Database.GetService<IMigrator>();
        var migrationIds = dbContext.Database.GetMigrations().ToArray();
        var latestMigration = Assert.Single(migrationIds, id => id.EndsWith("_AddBlocoAndUnidade", StringComparison.Ordinal));
        var previousMigration = migrationIds[^2];

        await migrator.MigrateAsync(latestMigration);
        await migrator.MigrateAsync(previousMigration);

        Assert.False(await RelationExistsAsync("bloco"));
        Assert.False(await RelationExistsAsync("unidade"));
        Assert.Null(await GetIndexDefinitionAsync("idx_bloco_nome_ativo_unique"));
        Assert.Null(await GetIndexDefinitionAsync("idx_unidade_canonica_ativa"));

        await migrator.MigrateAsync(latestMigration);
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        return new AppDbContext(options);
    }

    private async Task<List<(string ColumnName, string DataType)>> GetColumnDefinitionsAsync(string tableName)
    {
        const string sql = """
            SELECT column_name, data_type
            FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = @tableName
            ORDER BY ordinal_position;
            """;

        var columns = new List<(string ColumnName, string DataType)>();

        await using var connection = await fixture.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tableName", tableName);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add((reader.GetString(0), reader.GetString(1)));
        }

        return columns;
    }

    private async Task<string?> GetIndexDefinitionAsync(string indexName)
    {
        const string sql = """
            SELECT indexdef
            FROM pg_indexes
            WHERE schemaname = 'public' AND indexname = @indexName;
            """;

        await using var connection = await fixture.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("indexName", indexName);

        var result = await command.ExecuteScalarAsync();
        return result as string;
    }

    private async Task<bool> RelationExistsAsync(string relationName)
    {
        const string sql = "SELECT to_regclass(@qualifiedName) IS NOT NULL;";

        await using var connection = await fixture.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("qualifiedName", $"public.{relationName}");

        return (bool)(await command.ExecuteScalarAsync())!;
    }
}
