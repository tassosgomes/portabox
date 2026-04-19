using System.Diagnostics.Metrics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Infrastructure;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.Observability;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao;
using PortaBox.Modules.Gestao.Application.Commands.CreateCondominio;

namespace PortaBox.Api.IntegrationTests;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class ObservabilityIntegrationTests(
    PostgresDatabaseFixture postgresFixture,
    MinioFixture minioFixture,
    MailHogFixture mailHogFixture)
    : IClassFixture<MinioFixture>, IClassFixture<MailHogFixture>
{
    [Fact]
    public async Task HealthReady_ShouldReturn200_WhenAllServicesAccessible()
    {
        await using var factory = CreateReadinessFactory(
            postgresFixture.ConnectionString,
            minioEndpoint: minioFixture.Endpoint,
            minioBucketName: minioFixture.BucketName,
            smtpHost: mailHogFixture.Hostname,
            smtpPort: mailHogFixture.SmtpMappedPort);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync();
        var payload = await JsonDocument.ParseAsync(stream);
        Assert.Equal("Healthy", payload.RootElement.GetProperty("status").GetString());
        Assert.True(payload.RootElement.TryGetProperty("checks", out _));
    }

    [Fact]
    public async Task HealthReady_ShouldReturn503_WhenStorageIsInaccessible()
    {
        // Uses the real MinIO server but points to a bucket that does not exist,
        // causing the storage check to report Unhealthy → overall readiness is 503.
        await using var factory = CreateReadinessFactory(
            postgresFixture.ConnectionString,
            minioEndpoint: minioFixture.Endpoint,
            minioBucketName: "non-existent-bucket-xyz",
            smtpHost: mailHogFixture.Hostname,
            smtpPort: mailHogFixture.SmtpMappedPort);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync();
        var payload = await JsonDocument.ParseAsync(stream);
        Assert.Equal("Unhealthy", payload.RootElement.GetProperty("status").GetString());
        var storageCheck = payload.RootElement.GetProperty("checks").GetProperty("storage");
        Assert.Equal("Unhealthy", storageCheck.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CreateCondominio_ShouldIncrementCondominioCreatedTotalCounter()
    {
        await postgresFixture.ResetAsync();

        var measurements = new List<long>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == PortaBoxDiagnostics.MeterName &&
                instrument.Name == "condominio_created_total")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, _, _) => measurements.Add(measurement));
        listener.Start();

        await using var context = await ObservabilityIntegrationContext.CreateAsync(postgresFixture.ConnectionString);
        var command = BuildCommand(context.OperatorUserId, $"metrics-{Guid.NewGuid():N}@portabox.test");
        var result = await context.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(1L, measurements);
    }

    private static WebApplicationFactory<Program> CreateReadinessFactory(
        string postgresConnectionString,
        string minioEndpoint,
        string minioBucketName,
        string smtpHost,
        int smtpPort)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings__Postgres"] = postgresConnectionString,
            ["Persistence__ApplyMigrationsOnStartup"] = "false",
            ["Storage__Provider"] = "Minio",
            ["Storage__Endpoint"] = minioEndpoint,
            ["Storage__BucketName"] = minioBucketName,
            ["Storage__AccessKey"] = "minioadmin",
            ["Storage__SecretKey"] = "minioadmin",
            ["Storage__UseSsl"] = "false",
            ["Storage__ForcePathStyle"] = "true",
            ["Storage__Region"] = "us-east-1",
            ["Email__Host"] = smtpHost,
            ["Email__Port"] = smtpPort.ToString(),
            ["DomainEvents__Publisher__Enabled"] = "false"
        };

        return new EnvVarWebApplicationFactory("Development", settings);
    }

    private static CreateCondominioCommand BuildCommand(Guid operatorUserId, string sindicoEmail) =>
        new(
            operatorUserId,
            "Residencial Observabilidade",
            "45.723.174/0001-10",
            "Rua da Métrica",
            "42",
            null,
            "Centro",
            "São Paulo",
            "SP",
            "01310100",
            null,
            new DateOnly(2026, 4, 1),
            "Maioria simples",
            "Carlos da Silva",
            "529.982.247-25",
            new DateOnly(2026, 4, 2),
            "Ana Oliveira",
            sindicoEmail,
            "+5511999880001");

    private sealed class EnvVarWebApplicationFactory(
        string environment,
        IReadOnlyDictionary<string, string?> settings) : WebApplicationFactory<Program>
    {
        private readonly Dictionary<string, string?> _previousValues = [];

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            foreach (var pair in settings)
            {
                _previousValues[pair.Key] = Environment.GetEnvironmentVariable(pair.Key);
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }

            builder.UseEnvironment(environment);
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

        private void RestoreEnvironmentVariables()
        {
            foreach (var pair in _previousValues)
            {
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }
        }
    }

    private sealed class ObservabilityIntegrationContext : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly AsyncServiceScope _scope;
        private readonly string? _previousEnvironment;

        public Guid OperatorUserId { get; }

        public ICommandHandler<CreateCondominioCommand, CreateCondominioResult> Handler { get; }

        private ObservabilityIntegrationContext(
            ServiceProvider serviceProvider,
            AsyncServiceScope scope,
            Guid operatorUserId,
            ICommandHandler<CreateCondominioCommand, CreateCondominioResult> handler,
            string? previousEnvironment)
        {
            _serviceProvider = serviceProvider;
            _scope = scope;
            OperatorUserId = operatorUserId;
            Handler = handler;
            _previousEnvironment = previousEnvironment;
        }

        public static async Task<ObservabilityIntegrationContext> CreateAsync(string connectionString)
        {
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

                var services = new ServiceCollection();
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
                    UserName = $"operator-obs-{Guid.NewGuid():N}@portabox.test",
                    NormalizedUserName = $"OPERATOR-OBS-{Guid.NewGuid():N}@PORTABOX.TEST",
                    Email = $"operator-obs-{Guid.NewGuid():N}@portabox.test",
                    NormalizedEmail = $"OPERATOR-OBS-{Guid.NewGuid():N}@PORTABOX.TEST",
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    ConcurrencyStamp = Guid.NewGuid().ToString("N")
                };

                dbContext.Users.Add(operatorUser);
                await dbContext.SaveChangesAsync();

                return new ObservabilityIntegrationContext(
                    serviceProvider,
                    scope,
                    operatorUser.Id,
                    scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateCondominioCommand, CreateCondominioResult>>(),
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
            await _scope.DisposeAsync();
            await _serviceProvider.DisposeAsync();
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _previousEnvironment);
        }
    }
}
