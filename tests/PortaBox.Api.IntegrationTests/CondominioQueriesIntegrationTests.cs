using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Infrastructure;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao;
using PortaBox.Modules.Gestao.Application.Commands.ActivateCondominio;
using PortaBox.Modules.Gestao.Application.Commands.CreateCondominio;
using PortaBox.Modules.Gestao.Application.Queries.GetCondominioDetails;
using PortaBox.Modules.Gestao.Application.Queries.ListCondominios;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Api.IntegrationTests;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class CondominioQueriesIntegrationTests(PostgresDatabaseFixture fixture)
{
    [Fact]
    public async Task ListCondominios_OperatorContext_ShouldReturnAllTenants()
    {
        await fixture.ResetAsync();
        await using var context = await QueryIntegrationContext.CreateAsync(fixture.ConnectionString, scopeTenantForQueries: false);
        await context.CreateTenantAsync("Residencial Bosque Azul", "12.345.678/0001-95", "+5585999990001");
        await context.CreateTenantAsync("Condominio Praia Norte", "45.723.174/0001-10", "+5585999990002");

        var result = await context.ListHandler.HandleAsync(new ListCondominiosQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.All(result.Value.Items, item => Assert.StartsWith("****", item.CnpjMasked, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ListCondominios_StatusAtivo_ShouldFilterResults()
    {
        await fixture.ResetAsync();
        await using var context = await QueryIntegrationContext.CreateAsync(fixture.ConnectionString, scopeTenantForQueries: false);
        var inactive = await context.CreateTenantAsync("Residencial Bosque Azul", "12.345.678/0001-95", "+5585999990001");
        var active = await context.CreateTenantAsync("Condominio Praia Norte", "45.723.174/0001-10", "+5585999990002");
        await context.ActivateAsync(active.CondominioId);

        var result = await context.ListHandler.HandleAsync(new ListCondominiosQuery(status: CondominioStatus.Ativo), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var items = Assert.Single(result.Value!.Items);
        Assert.Equal(active.CondominioId, items.Id);
        Assert.DoesNotContain(result.Value.Items, item => item.Id == inactive.CondominioId);
    }

    [Fact]
    public async Task ListCondominios_SearchTerm_ShouldMatchPartialNameUsingIlike()
    {
        await fixture.ResetAsync();
        await using var context = await QueryIntegrationContext.CreateAsync(fixture.ConnectionString, scopeTenantForQueries: false);
        await context.CreateTenantAsync("Residencial Bosque Azul", "12.345.678/0001-95", "+5585999990001");
        await context.CreateTenantAsync("Condominio Praia Norte", "45.723.174/0001-10", "+5585999990002");

        var result = await context.ListHandler.HandleAsync(new ListCondominiosQuery(searchTerm: "praia"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal("Condominio Praia Norte", item.NomeFantasia);
    }

    [Fact]
    public async Task GetCondominioDetails_OperatorContext_ShouldReturnCompleteMaskedDto()
    {
        await fixture.ResetAsync();
        await using var context = await QueryIntegrationContext.CreateAsync(fixture.ConnectionString, scopeTenantForQueries: false);
        var created = await context.CreateTenantAsync("Residencial Bosque Azul", "12.345.678/0001-95", "+5511999998888");
        await context.AddDocumentAsync(created.CondominioId, created.SindicoUserId);
        await context.AddAuditEntriesAsync(created.CondominioId, 25);

        var result = await context.DetailsHandler.HandleAsync(new GetCondominioDetailsQuery(created.CondominioId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var details = Assert.IsType<CondominioDetailsDto>(result.Value);
        Assert.Equal(created.CondominioId, details.Id);
        Assert.Equal("****8000195", details.CnpjMasked);
        Assert.NotNull(details.OptIn);
        Assert.Equal("***.456.789-**", details.OptIn!.SignatarioCpfMasked);
        Assert.NotNull(details.Sindico);
        Assert.Equal("+55 11 9****-8888", details.Sindico!.CelularMasked);
        Assert.True(details.Documentos.Count >= 1);
        Assert.Equal(20, details.AuditLog.Count);
        Assert.True(details.SindicoSenhaDefinida is false);
    }

    [Fact]
    public async Task GetCondominioDetails_SindicoFromAnotherTenant_ShouldReturnNotFound()
    {
        await fixture.ResetAsync();
        await using var operatorContext = await QueryIntegrationContext.CreateAsync(fixture.ConnectionString, scopeTenantForQueries: false);
        var first = await operatorContext.CreateTenantAsync("Residencial Bosque Azul", "12.345.678/0001-95", "+5585999990001");
        await operatorContext.CreateTenantAsync("Condominio Praia Norte", "45.723.174/0001-10", "+5585999990002");

        await using var sindicoContext = await QueryIntegrationContext.CreateAsync(
            fixture.ConnectionString,
            scopeTenantForQueries: true,
            forcedTenantId: Guid.NewGuid());

        using var tenantScope = sindicoContext.BeginForcedTenantScope();
        var result = await sindicoContext.DetailsHandler.HandleAsync(new GetCondominioDetailsQuery(first.CondominioId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("gestao.condominio.not_found", result.Error);
    }

    private sealed class QueryIntegrationContext : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly AsyncServiceScope _scope;
        private readonly string? _previousEnvironment;
        private readonly Guid? _forcedTenantId;

        private QueryIntegrationContext(
            ServiceProvider serviceProvider,
            AsyncServiceScope scope,
            AppDbContext dbContext,
            ICommandHandler<CreateCondominioCommand, CreateCondominioResult> createHandler,
            ICommandHandler<ActivateCondominioCommand, ActivateCondominioResult> activateHandler,
            IQueryHandler<ListCondominiosQuery, PortaBox.Modules.Gestao.Application.Common.PagedResult<CondominioListItemDto>> listHandler,
            IQueryHandler<GetCondominioDetailsQuery, CondominioDetailsDto> detailsHandler,
            AppUser operatorUser,
            Guid? forcedTenantId,
            string? previousEnvironment)
        {
            _serviceProvider = serviceProvider;
            _scope = scope;
            DbContext = dbContext;
            CreateHandler = createHandler;
            ActivateHandler = activateHandler;
            ListHandler = listHandler;
            DetailsHandler = detailsHandler;
            OperatorUser = operatorUser;
            _forcedTenantId = forcedTenantId;
            _previousEnvironment = previousEnvironment;
        }

        public AppDbContext DbContext { get; }

        public ICommandHandler<CreateCondominioCommand, CreateCondominioResult> CreateHandler { get; }

        public ICommandHandler<ActivateCondominioCommand, ActivateCondominioResult> ActivateHandler { get; }

        public IQueryHandler<ListCondominiosQuery, PortaBox.Modules.Gestao.Application.Common.PagedResult<CondominioListItemDto>> ListHandler { get; }

        public IQueryHandler<GetCondominioDetailsQuery, CondominioDetailsDto> DetailsHandler { get; }

        public AppUser OperatorUser { get; }

        public IDisposable? BeginForcedTenantScope()
        {
            return _forcedTenantId.HasValue
                ? _scope.ServiceProvider.GetRequiredService<ITenantContext>().BeginScope(_forcedTenantId.Value)
                : null;
        }

        public async Task<CreateCondominioResult> CreateTenantAsync(string nome, string cnpj, string celular)
        {
            var result = await CreateHandler.HandleAsync(
                new CreateCondominioCommand(
                    OperatorUser.Id,
                    nome,
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
                    celular),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            return result.Value!;
        }

        public async Task ActivateAsync(Guid condominioId)
        {
            var result = await ActivateHandler.HandleAsync(
                new ActivateCondominioCommand(condominioId, OperatorUser.Id, null),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
        }

        public async Task AddDocumentAsync(Guid condominioId, Guid uploadedByUserId)
        {
            DbContext.OptInDocuments.Add(OptInDocument.Create(
                Guid.NewGuid(),
                condominioId,
                OptInDocumentKind.Ata,
                $"tenants/{condominioId}/ata.pdf",
                "application/pdf",
                1024,
                new string('a', 64),
                uploadedByUserId,
                TimeProvider.System));

            await DbContext.SaveChangesAsync();
        }

        public async Task AddAuditEntriesAsync(Guid condominioId, int count)
        {
            for (var index = 0; index < count; index++)
            {
                DbContext.TenantAuditEntries.Add(TenantAuditEntry.Create(
                    condominioId,
                    TenantAuditEventKind.Other,
                    OperatorUser.Id,
                    DateTimeOffset.UtcNow.AddMinutes(index),
                    $"note-{index}"));
            }

            await DbContext.SaveChangesAsync();
        }

        public static async Task<QueryIntegrationContext> CreateAsync(string connectionString, bool scopeTenantForQueries, Guid? forcedTenantId = null)
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

                return new QueryIntegrationContext(
                    serviceProvider,
                    scope,
                    dbContext,
                    scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateCondominioCommand, CreateCondominioResult>>(),
                    scope.ServiceProvider.GetRequiredService<ICommandHandler<ActivateCondominioCommand, ActivateCondominioResult>>(),
                    scope.ServiceProvider.GetRequiredService<IQueryHandler<ListCondominiosQuery, PortaBox.Modules.Gestao.Application.Common.PagedResult<CondominioListItemDto>>>(),
                    scope.ServiceProvider.GetRequiredService<IQueryHandler<GetCondominioDetailsQuery, CondominioDetailsDto>>(),
                    operatorUser,
                    forcedTenantId,
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
