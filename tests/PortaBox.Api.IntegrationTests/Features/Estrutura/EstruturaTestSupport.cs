using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PortaBox.Api.IntegrationTests.Helpers;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao.Domain;
using PortaBox.Modules.Gestao.Domain.Blocos;
using PortaBox.Modules.Gestao.Domain.Unidades;

namespace PortaBox.Api.IntegrationTests.Features.Estrutura;

internal sealed class EstruturaTestApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _previousEnvironmentValues = [];

    public HttpClient CreateClient(TestAuthContext? authContext)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        if (authContext is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthContext.HeaderName, authContext.ToHeaderValue());
        }

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        foreach (var (key, value) in CreateEnvironmentSettings(connectionString))
        {
            _previousEnvironmentValues[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }

        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    _ => { });
        });
    }

    protected override void Dispose(bool disposing)
    {
        RestoreEnvironmentVariables();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        RestoreEnvironmentVariables();
        await base.DisposeAsync();
    }

    private static Dictionary<string, string?> CreateEnvironmentSettings(string postgresConnectionString)
    {
        return new Dictionary<string, string?>
        {
            ["ConnectionStrings__Postgres"] = postgresConnectionString,
            ["Persistence__ApplyMigrationsOnStartup"] = "false",
            ["Email__Provider"] = "Fake",
            ["DomainEvents__Publisher__Enabled"] = "false",
            ["CondominioMagicLink__SindicoAppBaseUrl"] = "https://sindico.portabox.test",
            ["RateLimiting__Auth__MaxRequests"] = "1000",
            ["Storage__Provider"] = "Minio",
            ["Storage__Endpoint"] = "http://127.0.0.1:9000",
            ["Storage__BucketName"] = "log-portaria-tests",
            ["Storage__AccessKey"] = "minioadmin",
            ["Storage__SecretKey"] = "minioadmin",
            ["Storage__UseSsl"] = "false",
            ["Storage__ForcePathStyle"] = "true",
            ["Storage__Region"] = "us-east-1"
        };
    }

    private void RestoreEnvironmentVariables()
    {
        foreach (var (key, value) in _previousEnvironmentValues)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "TestAuth";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(TestAuthContext.HeaderName, out var headerValue))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (!TestAuthContext.TryParse(headerValue.ToString(), out var authContext) || authContext is null)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid test auth header."));
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, authContext.UserId.ToString()),
                new(ClaimTypes.Name, authContext.UserId.ToString())
            };

            if (authContext.TenantId is { } tenantId)
            {
                claims.Add(new Claim("tenant_id", tenantId.ToString()));
            }

            claims.AddRange(authContext.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}

internal static class EstruturaTestData
{
    private static readonly string[] ValidCnpjs =
    [
        "12.345.678/0001-95",
        "45.723.174/0001-10",
        "04.252.011/0001-10",
        "03.778.130/0001-11",
        "71.506.168/0001-11",
        "48.719.269/0001-00",
        "22.621.265/0001-64",
        "62.231.031/0001-55",
        "57.412.978/0001-55",
        "87.389.296/0001-64"
    ];

    public static async Task<SeededTenant> SeedActiveTenantAsync(
        IServiceProvider services,
        string? tenantName = null,
        string? blocoNome = null,
        IReadOnlyList<(int Andar, string Numero)>? unidades = null,
        bool withStructure = true)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var cnpj = ValidCnpjs[await dbContext.Condominios.IgnoreQueryFilters().CountAsync() % ValidCnpjs.Length];
        var now = clock.GetUtcNow();
        var condominioId = Guid.NewGuid();
        var operatorUser = CreateUser($"operator-{Guid.NewGuid():N}@portabox.test", null);
        var sindicoUser = CreateUser($"sindico-{Guid.NewGuid():N}@portabox.test", condominioId);

        var condominio = Condominio.Create(
            condominioId,
            tenantName ?? $"Residencial {condominioId.ToString()[..6]}",
            cnpj,
            operatorUser.Id,
            clock,
            enderecoLogradouro: "Rua das Palmeiras",
            enderecoNumero: "123",
            enderecoBairro: "Centro",
            enderecoCidade: "Fortaleza",
            enderecoUf: "CE",
            enderecoCep: "60000000",
            administradoraNome: "Admin XPTO");
        condominio.TryActivate(operatorUser.Id, clock, out _);

        var sindico = Sindico.Create(
            Guid.NewGuid(),
            condominioId,
            sindicoUser.Id,
            "Sindico Teste",
            "+5585999990001",
            clock);

        dbContext.Users.AddRange(operatorUser, sindicoUser);
        dbContext.Condominios.Add(condominio);
        dbContext.Sindicos.Add(sindico);

        Bloco? bloco = null;
        var seededUnits = new List<SeededUnit>();
        if (withStructure)
        {
            bloco = CreateBloco(condominioId, blocoNome ?? "Bloco A", sindicoUser.Id, clock);
            dbContext.Blocos.Add(bloco);

            foreach (var (andar, numero) in unidades ?? DefaultUnits())
            {
                var unidade = CreateUnidade(condominioId, bloco, andar, numero, sindicoUser.Id, clock);
                seededUnits.Add(new SeededUnit(unidade.Id, unidade.Andar, unidade.Numero));
                dbContext.Unidades.Add(unidade);
            }
        }

        await dbContext.SaveChangesAsync();

        return new SeededTenant(
            condominioId,
            condominio.NomeFantasia,
            sindicoUser.Id,
            sindico.Id,
            operatorUser.Id,
            bloco?.Id,
            bloco?.Nome,
            seededUnits);
    }

    public static async Task<Bloco> AddBlocoAsync(IServiceProvider services, Guid condominioId, string nome, Guid performedByUserId)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var bloco = CreateBloco(condominioId, nome, performedByUserId, clock);
        dbContext.Blocos.Add(bloco);
        await dbContext.SaveChangesAsync();
        return bloco;
    }

    public static async Task<Unidade> AddUnidadeAsync(IServiceProvider services, Guid tenantId, Guid blocoId, int andar, string numero, Guid performedByUserId)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var bloco = await dbContext.Blocos.IgnoreQueryFilters().SingleAsync(current => current.Id == blocoId);
        var unidade = CreateUnidade(tenantId, bloco, andar, numero, performedByUserId, clock);

        dbContext.Unidades.Add(unidade);
        await dbContext.SaveChangesAsync();
        return unidade;
    }

    public static async Task ForceInactivateBlocoAsync(IServiceProvider services, Guid blocoId, Guid performedByUserId)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE bloco
            SET ativo = FALSE,
                inativado_em = {DateTime.UtcNow},
                inativado_por = {performedByUserId}
            WHERE id = {blocoId}
            """);
    }

    public static async Task ForceInactivateUnidadeAsync(IServiceProvider services, Guid unidadeId, Guid performedByUserId)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE unidade
            SET ativo = FALSE,
                inativado_em = {DateTime.UtcNow},
                inativado_por = {performedByUserId}
            WHERE id = {unidadeId}
            """);
    }

    public static async Task<IReadOnlyList<TenantAuditEntry>> GetAuditEntriesAsync(IServiceProvider services, Guid tenantId)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.TenantAuditEntries
            .AsNoTracking()
            .Where(entry => entry.TenantId == tenantId)
            .OrderBy(entry => entry.Id)
            .ToListAsync();
    }

    public static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private static IReadOnlyList<(int Andar, string Numero)> DefaultUnits()
    {
        return [(1, "101"), (1, "102"), (2, "201")];
    }

    private static AppUser CreateUser(string email, Guid? tenantId)
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            SindicoTenantId = tenantId
        };
    }

    private static Bloco CreateBloco(Guid condominioId, string nome, Guid performedByUserId, TimeProvider clock)
    {
        var result = Bloco.Create(Guid.NewGuid(), condominioId, condominioId, nome, performedByUserId, clock);
        return result.Value ?? throw new InvalidOperationException(result.Error ?? "Falha ao criar bloco de teste.");
    }

    private static Unidade CreateUnidade(Guid tenantId, Bloco bloco, int andar, string numero, Guid performedByUserId, TimeProvider clock)
    {
        var result = Unidade.Create(Guid.NewGuid(), tenantId, bloco, andar, numero, performedByUserId, clock);
        return result.Value ?? throw new InvalidOperationException(result.Error ?? "Falha ao criar unidade de teste.");
    }
}

internal sealed record SeededTenant(
    Guid CondominioId,
    string NomeFantasia,
    Guid SindicoUserId,
    Guid SindicoId,
    Guid OperatorUserId,
    Guid? BlocoId,
    string? BlocoNome,
    IReadOnlyList<SeededUnit> Unidades);

internal sealed record SeededUnit(Guid Id, int Andar, string Numero);
