using Microsoft.EntityFrameworkCore;
using Npgsql;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.MultiTenancy;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Infrastructure.Repositories;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Api.IntegrationTests;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class OptInRecordPersistenceTests(PostgresDatabaseFixture fixture)
{
    [Fact]
    public async Task SavingSecondOptInRecordForSameTenant_ShouldViolateUniqueConstraint()
    {
        await fixture.ResetAsync();

        Guid tenantId;
        Guid userId;

        await using (var seedContext = BuildContext())
        {
            var registeredBy = await CreateUserAsync(seedContext, "operator-optin-unique");
            userId = registeredBy.Id;
            tenantId = Guid.NewGuid();

            seedContext.Condominios.Add(Condominio.Create(
                tenantId,
                "Condominio Horizonte",
                "12.345.678/0001-95",
                registeredBy.Id,
                TimeProvider.System));
            seedContext.OptInRecords.Add(BuildOptInRecord(tenantId, registeredBy.Id, "123.456.789-09"));

            await seedContext.SaveChangesAsync();
        }

        // Use a separate context so EF Core's identity cache doesn't swallow the duplicate.
        await using var secondContext = BuildContext();
        secondContext.OptInRecords.Add(BuildOptInRecord(tenantId, userId, "390.533.447-05"));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => secondContext.SaveChangesAsync());

        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
    }

    [Fact]
    public async Task DistinctTenants_ShouldPersistOneOptInRecordEach()
    {
        await fixture.ResetAsync();

        await using var dbContext = BuildContext();
        var registeredBy = await CreateUserAsync(dbContext, "operator-optin-multi");
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        dbContext.Condominios.AddRange(
            Condominio.Create(tenantA, "Condominio A", "12.345.678/0001-95", registeredBy.Id, TimeProvider.System),
            Condominio.Create(tenantB, "Condominio B", "45.723.174/0001-10", registeredBy.Id, TimeProvider.System));

        dbContext.OptInRecords.AddRange(
            BuildOptInRecord(tenantA, registeredBy.Id, "123.456.789-09"),
            BuildOptInRecord(tenantB, registeredBy.Id, "390.533.447-05"));

        await dbContext.SaveChangesAsync();

        var records = await dbContext.OptInRecords
            .IgnoreQueryFilters()
            .ToListAsync();

        Assert.Equal(2, records.Count);
        Assert.Contains(records, record => record.TenantId == tenantA && record.SignatarioCpf == "12345678909");
        Assert.Contains(records, record => record.TenantId == tenantB && record.SignatarioCpf == "39053344705");
    }

    [Fact]
    public async Task GetByTenantIdAsync_ShouldRespectTenantIsolation()
    {
        await fixture.ResetAsync();

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using (var seedContext = BuildContext())
        {
            var registeredBy = await CreateUserAsync(seedContext, "operator-optin-isolation");

            seedContext.Condominios.AddRange(
                Condominio.Create(tenantA, "Condominio A", "12.345.678/0001-95", registeredBy.Id, TimeProvider.System),
                Condominio.Create(tenantB, "Condominio B", "45.723.174/0001-10", registeredBy.Id, TimeProvider.System));

            seedContext.OptInRecords.AddRange(
                BuildOptInRecord(tenantA, registeredBy.Id, "123.456.789-09"),
                BuildOptInRecord(tenantB, registeredBy.Id, "390.533.447-05"));

            await seedContext.SaveChangesAsync();
        }

        var tenantContext = new TenantContext();
        using var scope = tenantContext.BeginScope(tenantB);

        await using var tenantDbContext = BuildContext(tenantContext);
        var repository = new OptInRecordRepository(tenantDbContext);

        var visibleRecord = await repository.GetByTenantIdAsync(tenantB);
        var hiddenRecord = await repository.GetByTenantIdAsync(tenantA);

        Assert.NotNull(visibleRecord);
        Assert.Equal(tenantB, visibleRecord.TenantId);
        Assert.Null(hiddenRecord);
    }

    private AppDbContext BuildContext(TenantContext? tenantContext = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        return new AppDbContext(options, tenantContext);
    }

    private static OptInRecord BuildOptInRecord(Guid tenantId, Guid registeredByUserId, string cpf)
    {
        return OptInRecord.Create(
            Guid.NewGuid(),
            tenantId,
            new DateOnly(2026, 4, 10),
            "Maioria simples",
            "Maria da Silva",
            cpf,
            new DateOnly(2026, 4, 11),
            registeredByUserId,
            TimeProvider.System);
    }

    private static AppUser BuildUser(string slug)
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{slug}@portabox.test",
            NormalizedUserName = $"{slug}@PORTABOX.TEST",
            Email = $"{slug}@portabox.test",
            NormalizedEmail = $"{slug}@PORTABOX.TEST",
            EmailConfirmed = true
        };
    }

    private static async Task<AppUser> CreateUserAsync(AppDbContext dbContext, string slug)
    {
        var user = BuildUser(slug);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user;
    }
}
