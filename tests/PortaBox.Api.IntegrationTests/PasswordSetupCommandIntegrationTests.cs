using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Application.Abstractions.MagicLinks;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Infrastructure;
using PortaBox.Infrastructure.Email;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.MagicLinks;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao;
using PortaBox.Modules.Gestao.Application.Commands.CreateCondominio;
using PortaBox.Modules.Gestao.Application.Commands.PasswordSetup;

namespace PortaBox.Api.IntegrationTests;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class PasswordSetupCommandIntegrationTests(PostgresDatabaseFixture fixture)
{
    [Fact]
    public async Task HandleAsync_HappyPath_ShouldSetPasswordConsumeTokenAndAllowSubsequentLoginValidation()
    {
        await fixture.ResetAsync();
        var loggerProvider = new ListLoggerProvider();
        await using var context = await IntegrationContext.CreateAsync(fixture.ConnectionString, loggerProvider);
        var created = await context.CreateTenantAsync();
        var token = context.ExtractLatestMagicLinkToken();

        var result = await context.PasswordSetupHandler.HandleAsync(
            new PasswordSetupCommand(token, "Validpass123", "127.0.0.1"),
            CancellationToken.None);

        var user = await context.DbContext.Users.SingleAsync(current => current.Id == created.SindicoUserId);
        var consumedMagicLink = await context.DbContext.MagicLinks
            .AsNoTracking()
            .SingleAsync(current => current.UserId == created.SindicoUserId);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
        Assert.NotNull(consumedMagicLink.ConsumedAt);
        Assert.True(await context.UserManager.CheckPasswordAsync(user, "Validpass123"));
        Assert.Contains(loggerProvider.Entries, entry => entry.Message.Contains("password-setup.succeeded", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandleAsync_ExpiredToken_ShouldReturnGenericFailureAndLogExpired()
    {
        await fixture.ResetAsync();
        var loggerProvider = new ListLoggerProvider();
        await using var context = await IntegrationContext.CreateAsync(fixture.ConnectionString, loggerProvider);
        await context.CreateTenantAsync();
        var token = context.ExtractLatestMagicLinkToken();
        var magicLink = await context.DbContext.MagicLinks.SingleAsync();
        await context.DbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE magic_link SET expires_at = {DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE id = {magicLink.Id}");

        var result = await context.PasswordSetupHandler.HandleAsync(
            new PasswordSetupCommand(token, "Validpass123", "127.0.0.1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PasswordSetupErrors.Generic, result.Error);
        Assert.Contains(loggerProvider.Entries, entry => entry.Message.Contains("reason_code=expired", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandleAsync_InvalidToken_ShouldReturnGenericFailureAndLogNotFound()
    {
        await fixture.ResetAsync();
        var loggerProvider = new ListLoggerProvider();
        await using var context = await IntegrationContext.CreateAsync(fixture.ConnectionString, loggerProvider);
        await context.CreateTenantAsync();

        var result = await context.PasswordSetupHandler.HandleAsync(
            new PasswordSetupCommand("invalid-token", "Validpass123", "127.0.0.1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PasswordSetupErrors.Generic, result.Error);
        Assert.Contains(loggerProvider.Entries, entry => entry.Message.Contains("reason_code=not_found", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandleAsync_SameTokenTwice_ShouldReturnGenericFailureAndLogAlreadyConsumed()
    {
        await fixture.ResetAsync();
        var loggerProvider = new ListLoggerProvider();
        await using var context = await IntegrationContext.CreateAsync(fixture.ConnectionString, loggerProvider);
        await context.CreateTenantAsync();
        var token = context.ExtractLatestMagicLinkToken();

        var first = await context.PasswordSetupHandler.HandleAsync(
            new PasswordSetupCommand(token, "Validpass123", "127.0.0.1"),
            CancellationToken.None);
        var second = await context.PasswordSetupHandler.HandleAsync(
            new PasswordSetupCommand(token, "Anothervalid123", "127.0.0.1"),
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal(PasswordSetupErrors.Generic, second.Error);
        Assert.Contains(loggerProvider.Entries, entry => entry.Message.Contains("reason_code=already_consumed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandleAsync_InvalidPasswordPolicy_ShouldReturnGenericFailureWithoutConsumingToken()
    {
        await fixture.ResetAsync();
        await using var context = await IntegrationContext.CreateAsync(fixture.ConnectionString);
        var created = await context.CreateTenantAsync();
        var token = context.ExtractLatestMagicLinkToken();

        var result = await context.PasswordSetupHandler.HandleAsync(
            new PasswordSetupCommand(token, "abcdefghij", "127.0.0.1"),
            CancellationToken.None);

        var magicLink = await context.DbContext.MagicLinks.SingleAsync(current => current.UserId == created.SindicoUserId);

        Assert.False(result.IsSuccess);
        Assert.Equal(PasswordSetupErrors.Generic, result.Error);
        Assert.Null(magicLink.ConsumedAt);
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
            ICommandHandler<CreateCondominioCommand, CreateCondominioResult> createHandler,
            ICommandHandler<PasswordSetupCommand, PasswordSetupResult> passwordSetupHandler,
            UserManager<AppUser> userManager,
            string? previousEnvironment)
        {
            _serviceProvider = serviceProvider;
            _scope = scope;
            DbContext = dbContext;
            CreateHandler = createHandler;
            PasswordSetupHandler = passwordSetupHandler;
            UserManager = userManager;
            _previousEnvironment = previousEnvironment;
        }

        public AppDbContext DbContext { get; }

        public ICommandHandler<CreateCondominioCommand, CreateCondominioResult> CreateHandler { get; }

        public ICommandHandler<PasswordSetupCommand, PasswordSetupResult> PasswordSetupHandler { get; }

        public UserManager<AppUser> UserManager { get; }

        public async Task<CreateCondominioResult> CreateTenantAsync()
        {
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

            DbContext.Users.Add(operatorUser);
            await DbContext.SaveChangesAsync();

            var result = await CreateHandler.HandleAsync(
                new CreateCondominioCommand(
                    operatorUser.Id,
                    "Residencial Bosque Azul",
                    "12.345.678/0001-95",
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
            return result.Value!;
        }

        public string ExtractLatestMagicLinkToken()
        {
            var message = _scope.ServiceProvider.GetRequiredService<FakeEmailSender>().SentMessages.Last();
            const string marker = "token=";
            var markerIndex = message.HtmlBody.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(markerIndex >= 0);

            var tokenStart = markerIndex + marker.Length;
            var tokenEnd = message.HtmlBody.IndexOfAny(['"', '\'', '<', ' ', '&'], tokenStart);
            if (tokenEnd < 0)
            {
                tokenEnd = message.HtmlBody.Length;
            }

            return Uri.UnescapeDataString(message.HtmlBody[tokenStart..tokenEnd]);
        }

        public static async Task<IntegrationContext> CreateAsync(string connectionString, ILoggerProvider? loggerProvider = null)
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
                        ["CondominioMagicLink:SindicoAppBaseUrl"] = "https://sindico.portabox.test",
                        ["Identity:Password:RequiredLength"] = "10",
                        ["Identity:Password:RequiredUniqueChars"] = "1",
                        ["Identity:Password:RequireDigit"] = "true",
                        ["Identity:Password:RequireLowercase"] = "false",
                        ["Identity:Password:RequireUppercase"] = "false",
                        ["Identity:Password:RequireNonAlphanumeric"] = "false"
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

                return new IntegrationContext(
                    serviceProvider,
                    scope,
                    dbContext,
                    scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateCondominioCommand, CreateCondominioResult>>(),
                    scope.ServiceProvider.GetRequiredService<ICommandHandler<PasswordSetupCommand, PasswordSetupResult>>(),
                    scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>(),
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

    private sealed class ListLoggerProvider : ILoggerProvider
    {
        private readonly List<LogEntry> _entries = [];

        public IReadOnlyList<LogEntry> Entries => _entries;

        public ILogger CreateLogger(string categoryName) => new ListLogger(_entries);

        public void Dispose()
        {
        }

        public sealed record LogEntry(string Message);

        private sealed class ListLogger(List<LogEntry> entries) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                entries.Add(new LogEntry(formatter(state, exception)));
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
