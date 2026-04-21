using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Application.Abstractions.Events;
using PortaBox.Domain.Abstractions;
using PortaBox.Infrastructure;
using PortaBox.Infrastructure.Events;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Api.IntegrationTests;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class DomainEventOutboxIntegrationTests(PostgresDatabaseFixture fixture)
{
    [Fact]
    public async Task FailedCommit_ShouldNotPersistDomainEventOutboxEntry()
    {
        await fixture.ResetAsync();

        await using var seedContext = BuildContext();
        var createdBy = await CreateUserAsync(seedContext, "operator-domain-outbox");
        seedContext.Condominios.Add(Condominio.Create(
            Guid.NewGuid(),
            "Condominio Base",
            "12.345.678/0001-95",
            createdBy.Id,
            TimeProvider.System));
        await seedContext.SaveChangesAsync();

        await using var failingContext = BuildContext();
        var duplicate = Condominio.Create(
            Guid.NewGuid(),
            "Condominio Duplicado",
            "12.345.678/0001-95",
            createdBy.Id,
            TimeProvider.System);

        AddDomainEvent(duplicate, new TestDomainEvent("condominio.cadastrado.v1", duplicate.NomeFantasia));
        failingContext.Condominios.Add(duplicate);

        await Assert.ThrowsAsync<DbUpdateException>(() => failingContext.SaveChangesAsync());

        await using var verificationContext = BuildContextWithoutInterceptors();
        Assert.Equal(0, await verificationContext.DomainEventOutboxEntries.CountAsync());
    }

    [Fact]
    public async Task SuccessfulCommit_ShouldPersistOneOutboxRowPerDomainEvent()
    {
        await fixture.ResetAsync();

        await using var dbContext = BuildContext();
        var createdBy = await CreateUserAsync(dbContext, "operator-domain-success");
        var condominio = Condominio.Create(
            Guid.NewGuid(),
            "Residencial Horizonte",
            "45.723.174/0001-10",
            createdBy.Id,
            TimeProvider.System);

        AddDomainEvent(condominio, new TestDomainEvent("condominio.cadastrado.v1", condominio.NomeFantasia));
        AddDomainEvent(condominio, new TestDomainEvent("condominio.auditado.v1", condominio.NomeFantasia));
        dbContext.Condominios.Add(condominio);

        await dbContext.SaveChangesAsync();

        var persisted = await dbContext.DomainEventOutboxEntries
            .OrderBy(entry => entry.CreatedAt)
            .ToListAsync();

        Assert.Equal(2, persisted.Count);
        Assert.All(persisted, entry =>
        {
            Assert.Equal(condominio.Id, entry.AggregateId);
            Assert.Equal(condominio.Id, entry.TenantId);
            Assert.Null(entry.PublishedAt);
        });
    }

    [Fact]
    public async Task DomainEventOutboxPublisher_ShouldMarkPublishedAtInBatches()
    {
        await fixture.ResetAsync();
        var previousEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
            using var provider = BuildInfrastructureProvider(enabled: true, pollInterval: "00:00:00.200");
            var hostedService = provider.GetServices<IHostedService>().OfType<DomainEventOutboxPublisher>().Single();

            using (var scope = provider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.DomainEventOutboxEntries.Add(DomainEventOutboxEntry.Create(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "condominio.cadastrado.v1",
                    Guid.NewGuid(),
                    "{\"eventType\":\"condominio.cadastrado.v1\"}",
                    TimeProvider.System.GetUtcNow()));
                await dbContext.SaveChangesAsync();
            }

            await hostedService.StartAsync(CancellationToken.None);

            try
            {
                await WaitUntilAsync(async () =>
                {
                    using var scope = provider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    return await dbContext.DomainEventOutboxEntries.AnyAsync(entry => entry.PublishedAt != null);
                });

                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persisted = await dbContext.DomainEventOutboxEntries.SingleAsync();

                Assert.NotNull(persisted.PublishedAt);
            }
            finally
            {
                await hostedService.StopAsync(CancellationToken.None);
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", previousEnvironment);
        }
    }

    [Fact]
    public async Task DisabledPublisherFlag_ShouldKeepPublishedAtNull()
    {
        await fixture.ResetAsync();
        var previousEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
            using var provider = BuildInfrastructureProvider(enabled: false, pollInterval: "00:00:00.200");

            Assert.DoesNotContain(provider.GetServices<IHostedService>(), service => service is DomainEventOutboxPublisher);

            using (var scope = provider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.DomainEventOutboxEntries.Add(DomainEventOutboxEntry.Create(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "condominio.cadastrado.v1",
                    Guid.NewGuid(),
                    "{\"eventType\":\"condominio.cadastrado.v1\"}",
                    TimeProvider.System.GetUtcNow()));
                await dbContext.SaveChangesAsync();
            }

            await Task.Delay(500);

            using var verificationScope = provider.CreateScope();
            var verificationContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var persisted = await verificationContext.DomainEventOutboxEntries.SingleAsync();

            Assert.Null(persisted.PublishedAt);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", previousEnvironment);
        }
    }

    [Fact]
    public async Task Migration_ShouldCreateDomainEventOutboxTableAndIndex()
    {
        await fixture.ResetAsync();
        await using var connection = await fixture.OpenConnectionAsync();
        await using var tableCommand = new Npgsql.NpgsqlCommand(
            """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_name = 'domain_event_outbox';
            """,
            connection);
        await using var indexCommand = new Npgsql.NpgsqlCommand(
            """
            SELECT indexdef
            FROM pg_indexes
            WHERE schemaname = 'public'
              AND tablename = 'domain_event_outbox'
              AND indexname = 'idx_domain_event_outbox_published_at_created_at';
            """,
            connection);

        var tableCount = (long)(await tableCommand.ExecuteScalarAsync() ?? 0L);
        var indexDefinition = await indexCommand.ExecuteScalarAsync();

        Assert.Equal(1L, tableCount);
        Assert.NotNull(indexDefinition);
        Assert.Contains("(published_at, created_at)", indexDefinition!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private AppDbContext BuildContext()
    {
        return new AppDbContext(BuildOptions(addInterceptors: true));
    }

    private AppDbContext BuildContextWithoutInterceptors()
    {
        return new AppDbContext(BuildOptions(addInterceptors: false));
    }

    private DbContextOptions<AppDbContext> BuildOptions(bool addInterceptors)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning));

        if (addInterceptors)
        {
            optionsBuilder.AddInterceptors(new DomainEventOutboxInterceptor(new NoOpDispatcher(), TimeProvider.System));
        }

        return optionsBuilder.Options;
    }

    private ServiceProvider BuildInfrastructureProvider(bool enabled, string pollInterval)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = fixture.ConnectionString,
                ["Storage:Provider"] = "Minio",
                ["Email:Provider"] = "Fake",
                ["DomainEvents:Publisher:Enabled"] = enabled.ToString(),
                ["DomainEvents:Publisher:BatchSize"] = "100",
                ["DomainEvents:Publisher:PollInterval"] = pollInterval
            })
            .Build();

        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    private static void AddDomainEvent(AggregateRoot aggregateRoot, IDomainEvent domainEvent)
    {
        typeof(AggregateRoot)
            .GetMethod("AddDomainEvent", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(aggregateRoot, [domainEvent]);
    }

    private static async Task<AppUser> CreateUserAsync(AppDbContext dbContext, string slug)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{slug}@portabox.test",
            NormalizedUserName = $"{slug}@PORTABOX.TEST",
            Email = $"{slug}@portabox.test",
            NormalizedEmail = $"{slug}@PORTABOX.TEST",
            EmailConfirmed = true
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> predicate, int maxAttempts = 20, int delayMilliseconds = 200)
    {
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (await predicate())
            {
                return;
            }

            await Task.Delay(delayMilliseconds);
        }

        throw new TimeoutException("Condition was not satisfied within the expected time.");
    }

    private sealed class NoOpDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed record TestDomainEvent(string EventType, string AggregateName) : IDomainEvent
    {
        public DateTimeOffset OccurredAt { get; } = new(2026, 4, 18, 12, 0, 0, TimeSpan.Zero);
    }
}
