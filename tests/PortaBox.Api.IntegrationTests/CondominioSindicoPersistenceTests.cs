using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.MultiTenancy;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Infrastructure.Repositories;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Api.IntegrationTests;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class CondominioSindicoPersistenceTests(PostgresDatabaseFixture fixture)
{
    [Fact]
    public async Task SavingDuplicateCnpj_ShouldViolateUniqueConstraint()
    {
        await fixture.ResetAsync();

        await using var dbContext = BuildContext();
        var createdBy = await CreateUserAsync(dbContext, "operator-duplicate");

        var first = Condominio.Create(
            Guid.NewGuid(),
            "Condominio Aurora",
            "12.345.678/0001-95",
            createdBy.Id,
            TimeProvider.System);

        var second = Condominio.Create(
            Guid.NewGuid(),
            "Condominio Aurora B",
            "12345678000195",
            createdBy.Id,
            TimeProvider.System);

        dbContext.Condominios.Add(first);
        await dbContext.SaveChangesAsync();

        dbContext.Condominios.Add(second);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());

        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
    }

    [Fact]
    public async Task ExistsByCnpjAsync_ShouldReturnTrueForExistingAndFalseForMissing()
    {
        await fixture.ResetAsync();

        await using var dbContext = BuildContext();
        var createdBy = await CreateUserAsync(dbContext, "operator-exists");
        var repository = new CondominioRepository(dbContext);

        var condominio = Condominio.Create(
            Guid.NewGuid(),
            "Condominio Horizonte",
            "12.345.678/0001-95",
            createdBy.Id,
            TimeProvider.System);

        await repository.AddAsync(condominio);
        await dbContext.SaveChangesAsync();

        Assert.True(await repository.ExistsByCnpjAsync("12.345.678/0001-95"));
        Assert.False(await repository.ExistsByCnpjAsync("45.723.174/0001-10"));
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldRespectTenantIsolation()
    {
        await fixture.ResetAsync();

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using (var seedContext = BuildContext())
        {
            var createdBy = BuildUser("operator-sindico");
            var userA = BuildUser("sindico-a", tenantA);
            var userB = BuildUser("sindico-b", tenantB);

            seedContext.Users.AddRange(createdBy, userA, userB);

            seedContext.Condominios.AddRange(
                Condominio.Create(tenantA, "Condominio A", "12.345.678/0001-95", createdBy.Id, TimeProvider.System),
                Condominio.Create(tenantB, "Condominio B", "45.723.174/0001-10", createdBy.Id, TimeProvider.System));

            seedContext.Sindicos.AddRange(
                Sindico.Create(Guid.NewGuid(), tenantA, userA.Id, "Sindico A", "+5585999990001", TimeProvider.System),
                Sindico.Create(Guid.NewGuid(), tenantB, userB.Id, "Sindico B", "+5585999990002", TimeProvider.System));

            await seedContext.SaveChangesAsync();
        }

        var tenantContext = new TenantContext();
        using var scope = tenantContext.BeginScope(tenantA);

        await using var tenantDbContext = BuildContext(tenantContext);
        var repository = new SindicoRepository(tenantDbContext);

        var sindicoDoTenant = await repository.GetByUserIdAsync(
            await GetUserIdByEmailAsync("sindico-a@portabox.test"));

        var sindicoOutroTenant = await repository.GetByUserIdAsync(
            await GetUserIdByEmailAsync("sindico-b@portabox.test"));

        Assert.NotNull(sindicoDoTenant);
        Assert.Equal(tenantA, sindicoDoTenant.TenantId);
        Assert.Null(sindicoOutroTenant);
    }

    [Fact]
    public async Task Migration_ShouldApplyAndRollbackUsingPostgresFixture()
    {
        var databaseName = $"portabox_condominio_{Guid.NewGuid():N}";
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
                .Options;

            await using var dbContext = new AppDbContext(options);
            await dbContext.Database.MigrateAsync();

            await AssertTablesAndIndexesExistAsync(testDatabaseConnectionString);

            var migrator = dbContext.GetService<IMigrator>();
            await migrator.MigrateAsync("20260418042222_AddIdentityBaseline");

            await using var verificationConnection = new NpgsqlConnection(testDatabaseConnectionString);
            await verificationConnection.OpenAsync();

            await using var verificationCommand = new NpgsqlCommand(
                """
                SELECT COUNT(*)
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_name IN ('condominio', 'sindico');
                """,
                verificationConnection);

            var tableCount = (long)(await verificationCommand.ExecuteScalarAsync() ?? 0L);
            Assert.Equal(0L, tableCount);
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

    private AppDbContext BuildContext(TenantContext? tenantContext = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options, tenantContext);
    }

    private static AppUser BuildUser(string slug, Guid? tenantId = null)
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{slug}@portabox.test",
            NormalizedUserName = $"{slug}@PORTABOX.TEST",
            Email = $"{slug}@portabox.test",
            NormalizedEmail = $"{slug}@PORTABOX.TEST",
            EmailConfirmed = true,
            SindicoTenantId = tenantId
        };
    }

    private async Task<AppUser> CreateUserAsync(AppDbContext dbContext, string slug, Guid? tenantId = null)
    {
        var user = BuildUser(slug, tenantId);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user;
    }

    private async Task<Guid> GetUserIdByEmailAsync(string email)
    {
        await using var dbContext = BuildContext();

        var userId = await dbContext.Users
            .Where(user => user.Email == email)
            .Select(user => user.Id)
            .SingleAsync();

        return userId;
    }

    private async Task AssertTablesAndIndexesExistAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var tablesCommand = new NpgsqlCommand(
            """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_name IN ('condominio', 'sindico');
            """,
            connection);

        var tableCount = (long)(await tablesCommand.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(2L, tableCount);

        await using var indexesCommand = new NpgsqlCommand(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'public'
              AND indexname IN ('idx_condominio_cnpj_unique', 'idx_condominio_status', 'idx_sindico_tenant_id');
            """,
            connection);

        var indexes = new List<string>();

        await using var reader = await indexesCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexes.Add(reader.GetString(0));
        }

        Assert.Contains("idx_condominio_cnpj_unique", indexes);
        Assert.Contains("idx_condominio_status", indexes);
        Assert.Contains("idx_sindico_tenant_id", indexes);
    }

    private string BuildConnectionString(string database)
    {
        var builder = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = database
        };

        return builder.ConnectionString;
    }
}
