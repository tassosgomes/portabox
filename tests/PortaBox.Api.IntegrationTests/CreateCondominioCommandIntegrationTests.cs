using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Application.Abstractions.Email;
using PortaBox.Application.Abstractions.MagicLinks;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Infrastructure;
using PortaBox.Infrastructure.Email;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao;
using PortaBox.Modules.Gestao.Application.Commands.CreateCondominio;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Api.IntegrationTests;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class CreateCondominioCommandIntegrationTests(PostgresDatabaseFixture fixture)
{
    [Fact]
    public async Task HandleAsync_HappyPath_ShouldPersistEntitiesOutboxMagicLinkAndEmail()
    {
        await fixture.ResetAsync();
        await using var context = await IntegrationContext.CreateAsync(fixture.ConnectionString);
        var command = BuildCommand(context.OperatorUser.Id, $"happy-{Guid.NewGuid():N}@portabox.test");

        var result = await context.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var condominio = await context.DbContext.Condominios.SingleAsync();
        var sindico = await context.DbContext.Sindicos.IgnoreQueryFilters().SingleAsync();
        var optInRecord = await context.DbContext.OptInRecords.IgnoreQueryFilters().SingleAsync();
        var auditEntry = await context.DbContext.TenantAuditEntries.SingleAsync();
        var outboxEntry = await context.DbContext.DomainEventOutboxEntries.SingleAsync();
        var magicLink = await context.DbContext.MagicLinks.SingleAsync();
        var user = await context.DbContext.Users.SingleAsync(current => current.Id == result.Value!.SindicoUserId);
        var fakeSender = context.ServiceProvider.GetRequiredService<FakeEmailSender>();
        var sentEmail = Assert.Single(fakeSender.SentMessages);

        Assert.Equal(CondominioStatus.PreAtivo, condominio.Status);
        Assert.Equal(result.Value.CondominioId, condominio.Id);
        Assert.Equal(result.Value.CondominioId, sindico.TenantId);
        Assert.Equal(result.Value.SindicoUserId, sindico.UserId);
        Assert.Equal(result.Value.CondominioId, optInRecord.TenantId);
        Assert.Equal(TenantAuditEventKind.Created, auditEntry.EventKind);
        Assert.Equal("condominio.cadastrado.v1", outboxEntry.EventType);
        Assert.Equal(result.Value.CondominioId, outboxEntry.AggregateId);
        Assert.Equal(result.Value.SindicoUserId, magicLink.UserId);
        Assert.Equal(MagicLinkPurpose.PasswordSetup, magicLink.Purpose);
        Assert.Equal(TimeSpan.FromHours(72), magicLink.ExpiresAt - magicLink.CreatedAt);
        Assert.Null(user.PasswordHash);
        Assert.Equal(command.SindicoEmail, sentEmail.To);
        Assert.Contains("password-setup?token=", sentEmail.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_DuplicateSindicoEmail_ShouldReturnFailure()
    {
        await fixture.ResetAsync();
        var repeatedEmail = $"duplicate-{Guid.NewGuid():N}@portabox.test";

        await using (var seedContext = await IntegrationContext.CreateAsync(fixture.ConnectionString))
        {
            var firstResult = await seedContext.Handler.HandleAsync(
                BuildCommand(seedContext.OperatorUser.Id, repeatedEmail, cnpj: "12.345.678/0001-95"),
                CancellationToken.None);
            Assert.True(firstResult.IsSuccess);
        }

        await using var verificationContext = await IntegrationContext.CreateAsync(fixture.ConnectionString);
        var result = await verificationContext.Handler.HandleAsync(
            BuildCommand(verificationContext.OperatorUser.Id, repeatedEmail, cnpj: "45.723.174/0001-10"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateCondominioErrors.SindicoEmailAlreadyExists, result.Error);
    }

    [Fact]
    public async Task HandleAsync_FailingOptInInsert_ShouldRollbackCondominioAndUser()
    {
        await fixture.ResetAsync();
        await using var seedContext = await IntegrationContext.CreateAsync(fixture.ConnectionString, failOptInAdd: true);

        var failingEmail = $"rollback-{Guid.NewGuid():N}@portabox.test";
        var command = BuildCommand(seedContext.OperatorUser.Id, failingEmail, cnpj: "04.252.011/0001-10");

        await Assert.ThrowsAsync<DbUpdateException>(() => seedContext.Handler.HandleAsync(command, CancellationToken.None));

        Assert.Equal(0, await seedContext.DbContext.Condominios.CountAsync());
        Assert.Equal(0, await seedContext.DbContext.OptInRecords.IgnoreQueryFilters().CountAsync());
        Assert.False(await seedContext.DbContext.Users.AnyAsync(user => user.Email == failingEmail));
        Assert.Empty(seedContext.ServiceProvider.GetRequiredService<FakeEmailSender>().SentMessages);
    }

    private static CreateCondominioCommand BuildCommand(
        Guid operatorUserId,
        string sindicoEmail,
        string cnpj = "12.345.678/0001-95",
        Guid? forcedTenantId = null)
    {
        return new CreateCondominioCommand(
            operatorUserId,
            "Residencial Bosque Azul",
            cnpj,
            "Rua das Palmeiras",
            "123",
            null,
            "Centro",
            "Fortaleza",
            "CE",
            "60000000",
            "Admin XPTO",
            new DateOnly(2026, 4, 10),
            "Maioria simples",
            "Maria da Silva",
            "123.456.789-09",
            new DateOnly(2026, 4, 11),
            "Joao da Silva",
            sindicoEmail,
            "+5585999990001");
    }

    private sealed class IntegrationContext : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly AsyncServiceScope _scope;
        private readonly string? _previousEnvironment;

        private IntegrationContext(
            ServiceProvider serviceProvider,
            AsyncServiceScope scope,
            AppDbContext dbContext,
            ICommandHandler<CreateCondominioCommand, CreateCondominioResult> handler,
            AppUser operatorUser,
            string? previousEnvironment)
        {
            _serviceProvider = serviceProvider;
            _scope = scope;
            DbContext = dbContext;
            Handler = handler;
            OperatorUser = operatorUser;
            _previousEnvironment = previousEnvironment;
        }

        public IServiceProvider ServiceProvider => _scope.ServiceProvider;

        public AppDbContext DbContext { get; }

        public ICommandHandler<CreateCondominioCommand, CreateCondominioResult> Handler { get; }

        public AppUser OperatorUser { get; }

        public static async Task<IntegrationContext> CreateAsync(string connectionString, bool failOptInAdd = false)
        {
            var services = new ServiceCollection();
            var previousEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

            try
            {
                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
                    {
                        ["ConnectionStrings:Postgres"] = connectionString,
                        ["Storage:Provider"] = "Minio",
                        ["Email:Provider"] = "Fake",
                        ["DomainEvents:Publisher:Enabled"] = "false",
                        ["CondominioMagicLink:SindicoAppBaseUrl"] = "https://sindico.portabox.test"
                    })
                    .Build();

                services.AddLogging();
                services.AddInfrastructure(configuration);
                services.AddPortaBoxModuleGestao(configuration);

                if (failOptInAdd)
                {
                    services.AddScoped<PortaBox.Modules.Gestao.Application.Repositories.IOptInRecordRepository, FailingOptInRecordRepository>();
                }

                var serviceProvider = services.BuildServiceProvider();
                var scope = serviceProvider.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Database.MigrateAsync();

                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
                if (!await roleManager.RoleExistsAsync(IdentityRoles.Sindico))
                {
                    await roleManager.CreateAsync(new AppRole
                    {
                        Id = Guid.NewGuid(),
                        Name = IdentityRoles.Sindico,
                        NormalizedName = IdentityRoles.Sindico.ToUpperInvariant()
                    });
                }

                var operatorUser = new AppUser
                {
                    Id = Guid.NewGuid(),
                    UserName = $"operator-{Guid.NewGuid():N}@portabox.test",
                    NormalizedUserName = $"OPERATOR-{Guid.NewGuid():N}@PORTABOX.TEST",
                    Email = $"operator-{Guid.NewGuid():N}@portabox.test",
                    NormalizedEmail = $"OPERATOR-{Guid.NewGuid():N}@PORTABOX.TEST",
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    ConcurrencyStamp = Guid.NewGuid().ToString("N")
                };

                dbContext.Users.Add(operatorUser);
                await dbContext.SaveChangesAsync();

                return new IntegrationContext(
                    serviceProvider,
                    scope,
                    dbContext,
                    scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateCondominioCommand, CreateCondominioResult>>(),
                    operatorUser,
                    previousEnvironment);
            }
            catch
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", previousEnvironment);
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _scope.DisposeAsync();
            await _serviceProvider.DisposeAsync();
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _previousEnvironment);
        }

        private sealed class FailingOptInRecordRepository : PortaBox.Modules.Gestao.Application.Repositories.IOptInRecordRepository
        {
            public Task AddAsync(OptInRecord optInRecord, CancellationToken cancellationToken = default)
            {
                throw new DbUpdateException("Simulated opt-in violation.", new PostgresException(
                    messageText: "duplicate key value violates unique constraint",
                    severity: "ERROR",
                    invariantSeverity: "ERROR",
                    sqlState: PostgresErrorCodes.UniqueViolation));
            }

            public Task<OptInRecord?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<OptInRecord?>(null);
            }
        }
    }
}
