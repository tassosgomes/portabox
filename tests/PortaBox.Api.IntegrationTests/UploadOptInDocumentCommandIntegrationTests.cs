using Amazon.S3;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Application.Abstractions.Persistence;
using PortaBox.Infrastructure;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.MultiTenancy;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Infrastructure.Repositories;
using PortaBox.Modules.Gestao;
using PortaBox.Modules.Gestao.Application.Commands.UploadOptInDocument;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Api.IntegrationTests;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class UploadOptInDocumentCommandIntegrationTests(
    PostgresDatabaseFixture postgresFixture,
    MinioFixture minioFixture) : IClassFixture<MinioFixture>
{
    [Fact]
    public async Task HandleAsync_HappyPath_ShouldUploadPdfToMinioAndPersistMetadata()
    {
        await postgresFixture.ResetAsync();
        await minioFixture.ResetAsync();
        await using var context = await UploadIntegrationContext.CreateAsync(postgresFixture.ConnectionString, minioFixture);
        var payload = Enumerable.Repeat((byte)0x2A, 1024 * 1024).ToArray();
        await using var stream = new MemoryStream(payload);

        var result = await context.Handler.HandleAsync(
            BuildCommand(context.Tenant.Id, context.OperatorUser.Id, stream, payload.LongLength),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var document = await context.DbContext.OptInDocuments.IgnoreQueryFilters().SingleAsync();
        using var client = minioFixture.CreateS3Client();
        using var response = await client.GetObjectAsync(minioFixture.BucketName, document.StorageKey);
        await using var downloaded = new MemoryStream();
        await response.ResponseStream.CopyToAsync(downloaded);

        Assert.Equal(payload, downloaded.ToArray());
        Assert.Equal("application/pdf", document.ContentType);
        Assert.Equal(payload.LongLength, document.SizeBytes);
        Assert.Equal(result.Value.DocumentId, document.Id);
    }

    [Fact]
    public async Task HandleAsync_WhenMetadataCommitFails_ShouldLogOrphanCandidate()
    {
        await postgresFixture.ResetAsync();
        await minioFixture.ResetAsync();

        var loggerProvider = new ListLoggerProvider();
        await using var context = await UploadIntegrationContext.CreateAsync(
            postgresFixture.ConnectionString,
            minioFixture,
            loggerProvider,
            failOnSaveChanges: true);
        var payload = new byte[] { 1, 2, 3, 4, 5 };
        await using var stream = new MemoryStream(payload);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.Handler.HandleAsync(
            BuildCommand(context.Tenant.Id, context.OperatorUser.Id, stream, payload.LongLength),
            CancellationToken.None));

        Assert.Equal("Simulated opt-in metadata failure.", exception.Message);
        Assert.Equal(0, await context.DbContext.OptInDocuments.IgnoreQueryFilters().CountAsync());
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.LogLevel == LogLevel.Error &&
            entry.Message.Contains("storage.orphan-candidate", StringComparison.Ordinal) &&
            entry.Message.Contains("condominios/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandleAsync_ElevenMegabyteFile_ShouldBeRejectedBeforeTouchingMinio()
    {
        await postgresFixture.ResetAsync();
        await minioFixture.ResetAsync();
        await using var context = await UploadIntegrationContext.CreateAsync(postgresFixture.ConnectionString, minioFixture);
        var payload = new byte[(11 * 1024 * 1024)];
        await using var stream = new MemoryStream(payload);
        using var client = minioFixture.CreateS3Client();

        await Assert.ThrowsAsync<ValidationException>(() => context.Handler.HandleAsync(
            BuildCommand(context.Tenant.Id, context.OperatorUser.Id, stream, payload.LongLength),
            CancellationToken.None));

        var objects = await client.ListObjectsV2Async(new Amazon.S3.Model.ListObjectsV2Request
        {
            BucketName = minioFixture.BucketName
        });

        Assert.Equal(0, await context.DbContext.OptInDocuments.IgnoreQueryFilters().CountAsync());
        Assert.Empty(objects.S3Objects);
    }

    [Fact]
    public async Task OptInDocumentRepository_ShouldRespectTenantIsolationAfterUpload()
    {
        await postgresFixture.ResetAsync();
        await minioFixture.ResetAsync();

        Guid tenantAId;
        Guid tenantBId;
        Guid documentId;

        await using (var context = await UploadIntegrationContext.CreateAsync(postgresFixture.ConnectionString, minioFixture))
        {
            var secondTenant = Condominio.Create(
                Guid.NewGuid(),
                "Residencial Horizonte",
                "45.723.174/0001-10",
                context.OperatorUser.Id,
                TimeProvider.System);

            context.DbContext.Condominios.Add(secondTenant);
            await context.DbContext.SaveChangesAsync();

            tenantAId = context.Tenant.Id;
            tenantBId = secondTenant.Id;

            var payload = new byte[] { 7, 8, 9, 10 };
            await using var stream = new MemoryStream(payload);
            var uploadResult = await context.Handler.HandleAsync(
                BuildCommand(tenantAId, context.OperatorUser.Id, stream, payload.LongLength),
                CancellationToken.None);

            Assert.True(uploadResult.IsSuccess);
            documentId = uploadResult.Value!.DocumentId;
        }

        var tenantContext = new TenantContext();
        using var scope = tenantContext.BeginScope(tenantBId);
        await using var tenantDbContext = BuildContext(postgresFixture.ConnectionString, tenantContext);
        var repository = new OptInDocumentRepository(tenantDbContext);

        var visibleToTenantB = await tenantDbContext.OptInDocuments.ToListAsync();
        var hiddenDocument = await repository.GetByIdAsync(documentId);

        Assert.Empty(visibleToTenantB);
        Assert.Null(hiddenDocument);
    }

    private static UploadOptInDocumentCommand BuildCommand(Guid tenantId, Guid uploadedByUserId, Stream stream, long sizeBytes)
    {
        return new UploadOptInDocumentCommand(
            tenantId,
            OptInDocumentKind.Ata,
            "application/pdf",
            "ata.pdf",
            sizeBytes,
            stream,
            uploadedByUserId);
    }

    private static AppDbContext BuildContext(string connectionString, ITenantContext? tenantContext = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        return new AppDbContext(options, tenantContext);
    }

    private sealed class UploadIntegrationContext : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly AsyncServiceScope _scope;
        private readonly string? _previousEnvironment;

        private UploadIntegrationContext(
            ServiceProvider serviceProvider,
            AsyncServiceScope scope,
            AppDbContext dbContext,
            ICommandHandler<UploadOptInDocumentCommand, UploadOptInDocumentResult> handler,
            AppUser operatorUser,
            Condominio tenant,
            string? previousEnvironment)
        {
            _serviceProvider = serviceProvider;
            _scope = scope;
            DbContext = dbContext;
            Handler = handler;
            OperatorUser = operatorUser;
            Tenant = tenant;
            _previousEnvironment = previousEnvironment;
        }

        public AppDbContext DbContext { get; }

        public ICommandHandler<UploadOptInDocumentCommand, UploadOptInDocumentResult> Handler { get; }

        public AppUser OperatorUser { get; }

        public Condominio Tenant { get; }

        public static async Task<UploadIntegrationContext> CreateAsync(
            string connectionString,
            MinioFixture minioFixture,
            ILoggerProvider? loggerProvider = null,
            bool failOnSaveChanges = false)
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
                        ["Storage:BucketName"] = minioFixture.BucketName,
                        ["Storage:Endpoint"] = minioFixture.Endpoint,
                        ["Storage:AccessKey"] = minioFixture.CreateOptions().AccessKey,
                        ["Storage:SecretKey"] = minioFixture.CreateOptions().SecretKey,
                        ["Storage:UseSsl"] = "false",
                        ["Email:Provider"] = "Fake",
                        ["DomainEvents:Publisher:Enabled"] = "false",
                        ["CondominioMagicLink:SindicoAppBaseUrl"] = "https://sindico.portabox.test"
                    })
                    .Build();

                services.AddLogging(builder =>
                {
                    builder.ClearProviders();

                    if (loggerProvider is not null)
                    {
                        builder.AddProvider(loggerProvider);
                    }
                });

                services.AddInfrastructure(configuration);
                services.AddPortaBoxModuleGestao(configuration);

                if (failOnSaveChanges)
                {
                    services.AddScoped<IApplicationDbSession, FailingApplicationDbSession>();
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

                var tenant = Condominio.Create(
                    Guid.NewGuid(),
                    "Residencial Bosque Azul",
                    "12.345.678/0001-95",
                    operatorUser.Id,
                    TimeProvider.System);

                dbContext.Users.Add(operatorUser);
                dbContext.Condominios.Add(tenant);
                await dbContext.SaveChangesAsync();

                return new UploadIntegrationContext(
                    serviceProvider,
                    scope,
                    dbContext,
                    scope.ServiceProvider.GetRequiredService<ICommandHandler<UploadOptInDocumentCommand, UploadOptInDocumentResult>>(),
                    operatorUser,
                    tenant,
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

        private sealed class FailingApplicationDbSession(AppDbContext dbContext) : IApplicationDbSession
        {
            public Task<IApplicationDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                dbContext.ChangeTracker.DetectChanges();
                throw new DbUpdateException("Simulated opt-in metadata failure.");
            }
        }
    }

    private sealed class ListLoggerProvider : ILoggerProvider
    {
        private readonly List<LogEntry> _entries = [];

        public IReadOnlyList<LogEntry> Entries => _entries;

        public ILogger CreateLogger(string categoryName)
        {
            return new ListLogger(_entries);
        }

        public void Dispose()
        {
        }

        public sealed record LogEntry(LogLevel LogLevel, string Message);

        private sealed class ListLogger(List<LogEntry> entries) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                entries.Add(new LogEntry(logLevel, formatter(state, exception)));
            }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new();

                public void Dispose()
                {
                }
            }
        }
    }
}
