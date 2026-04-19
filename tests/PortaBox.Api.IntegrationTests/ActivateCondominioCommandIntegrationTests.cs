using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Infrastructure;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao;
using PortaBox.Modules.Gestao.Application.Commands.ActivateCondominio;
using PortaBox.Modules.Gestao.Application.Commands.CreateCondominio;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Api.IntegrationTests;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class ActivateCondominioCommandIntegrationTests(PostgresDatabaseFixture fixture)
{
    [Fact]
    public async Task HandleAsync_PreAtivoTenant_ShouldActivateAndPersistAuditEntry()
    {
        await fixture.ResetAsync();
        await using var context = await IntegrationContext.CreateAsync(fixture.ConnectionString);
        var created = await context.CreateTenantAsync();

        var result = await context.ActivateHandler.HandleAsync(
            new ActivateCondominioCommand(created.CondominioId, context.OperatorUser.Id, "go live manual"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var condominio = await context.DbContext.Condominios.SingleAsync(current => current.Id == created.CondominioId);
        var activatedEntry = await context.DbContext.TenantAuditEntries
            .Where(current => current.TenantId == created.CondominioId)
            .Where(current => current.EventKind == TenantAuditEventKind.Activated)
            .SingleAsync();

        Assert.Equal(CondominioStatus.Ativo, condominio.Status);
        Assert.Equal(context.OperatorUser.Id, condominio.ActivatedByUserId);
        Assert.Equal(TenantAuditEventKind.Activated, activatedEntry.EventKind);
        Assert.Equal("go live manual", activatedEntry.Note);
    }

    [Fact]
    public async Task HandleAsync_AfterCreateAndActivate_ShouldKeepBothAuditEntriesForTenant()
    {
        await fixture.ResetAsync();
        await using var context = await IntegrationContext.CreateAsync(fixture.ConnectionString);
        var created = await context.CreateTenantAsync();

        var activation = await context.ActivateHandler.HandleAsync(
            new ActivateCondominioCommand(created.CondominioId, context.OperatorUser.Id, null),
            CancellationToken.None);

        Assert.True(activation.IsSuccess);

        var auditKinds = await context.DbContext.TenantAuditEntries
            .Where(current => current.TenantId == created.CondominioId)
            .OrderBy(current => current.Id)
            .Select(current => current.EventKind)
            .ToListAsync();

        Assert.Equal([TenantAuditEventKind.Created, TenantAuditEventKind.Activated], auditKinds);
    }

    [Fact]
    public async Task HandleAsync_AlreadyActiveTenant_ShouldNotAppendAuditEntry()
    {
        await fixture.ResetAsync();
        await using var context = await IntegrationContext.CreateAsync(fixture.ConnectionString);
        var created = await context.CreateTenantAsync();

        var firstResult = await context.ActivateHandler.HandleAsync(
            new ActivateCondominioCommand(created.CondominioId, context.OperatorUser.Id, null),
            CancellationToken.None);
        var auditCountBeforeRetry = await context.DbContext.TenantAuditEntries.CountAsync(current => current.TenantId == created.CondominioId);

        var secondResult = await context.ActivateHandler.HandleAsync(
            new ActivateCondominioCommand(created.CondominioId, context.OperatorUser.Id, "segunda tentativa"),
            CancellationToken.None);
        var auditCountAfterRetry = await context.DbContext.TenantAuditEntries.CountAsync(current => current.TenantId == created.CondominioId);

        Assert.True(firstResult.IsSuccess);
        Assert.False(secondResult.IsSuccess);
        Assert.Equal(ActivateCondominioErrors.AlreadyActive, secondResult.Error);
        Assert.Equal(auditCountBeforeRetry, auditCountAfterRetry);
    }

    [Fact]
    public async Task HandleAsync_Success_ShouldPersistCondominioAtivadoEventInOutbox()
    {
        await fixture.ResetAsync();
        await using var context = await IntegrationContext.CreateAsync(fixture.ConnectionString);
        var created = await context.CreateTenantAsync();

        var result = await context.ActivateHandler.HandleAsync(
            new ActivateCondominioCommand(created.CondominioId, context.OperatorUser.Id, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var outboxEntry = await context.DbContext.DomainEventOutboxEntries
            .Where(current => current.AggregateId == created.CondominioId)
            .Where(current => current.EventType == "condominio.ativado.v1")
            .SingleAsync();

        Assert.Contains("condominioId", outboxEntry.Payload, StringComparison.Ordinal);
        Assert.Contains(created.CondominioId.ToString(), outboxEntry.Payload, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class IntegrationContext : IAsyncDisposable
    {
        private static readonly string[] ValidCnpjs =
        [
            "12.345.678/0001-95",
            "45.723.174/0001-10",
            "04.252.011/0001-10",
            "03.778.130/0001-11"
        ];

        private readonly ServiceProvider _serviceProvider;
        private readonly AsyncServiceScope _scope;
        private readonly string? _previousEnvironment;

        private IntegrationContext(
            ServiceProvider serviceProvider,
            AsyncServiceScope scope,
            AppDbContext dbContext,
            ICommandHandler<CreateCondominioCommand, CreateCondominioResult> createHandler,
            ICommandHandler<ActivateCondominioCommand, ActivateCondominioResult> activateHandler,
            AppUser operatorUser,
            string? previousEnvironment)
        {
            _serviceProvider = serviceProvider;
            _scope = scope;
            DbContext = dbContext;
            CreateHandler = createHandler;
            ActivateHandler = activateHandler;
            OperatorUser = operatorUser;
            _previousEnvironment = previousEnvironment;
        }

        public AppDbContext DbContext { get; }

        public ICommandHandler<CreateCondominioCommand, CreateCondominioResult> CreateHandler { get; }

        public ICommandHandler<ActivateCondominioCommand, ActivateCondominioResult> ActivateHandler { get; }

        public AppUser OperatorUser { get; }

        public async Task<CreateCondominioResult> CreateTenantAsync()
        {
            var cnpj = ValidCnpjs[await DbContext.Condominios.CountAsync() % ValidCnpjs.Length];

            var result = await CreateHandler.HandleAsync(
                new CreateCondominioCommand(
                    OperatorUser.Id,
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
                    $"sindico-{Guid.NewGuid():N}@portabox.test",
                    "+5585999990001"),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            return result.Value!;
        }

        public static async Task<IntegrationContext> CreateAsync(string connectionString)
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
                    scope.ServiceProvider.GetRequiredService<ICommandHandler<ActivateCondominioCommand, ActivateCondominioResult>>(),
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
    }
}
