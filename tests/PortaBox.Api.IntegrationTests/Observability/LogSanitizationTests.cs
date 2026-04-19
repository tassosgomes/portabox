using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Application.Abstractions.Email;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Infrastructure;
using PortaBox.Infrastructure.Email;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao;
using PortaBox.Modules.Gestao.Application.Commands.CreateCondominio;
using PortaBox.Modules.Gestao.Application.Commands.PasswordSetup;

namespace PortaBox.Api.IntegrationTests.Observability;

/// <summary>
/// Verifies that a complete wizard flow (create condominio → password setup) emits no log
/// entries containing a raw token, password, full CPF, CNPJ, or e-mail address in clear text.
/// </summary>
[Collection(nameof(PostgresDatabaseCollection))]
public sealed class LogSanitizationTests(PostgresDatabaseFixture fixture)
{
    // Patterns that must NOT appear verbatim in any log message
    private const string SensitivePassword = "SensLog@Pass123";
    private const string SensitiveCpf = "52998224725";
    private const string SensitiveCnpj = "45723174000110";
    private static readonly string SensitiveSindicoEmail = $"sanitiz-{Guid.NewGuid():N}@portabox.test";

    [Fact]
    public async Task FullFlow_ShouldNotEmitTokenPasswordCpfOrEmailInLogs()
    {
        await fixture.ResetAsync();

        var logProvider = new CaptureLoggerProvider();
        await using var context = await TestServiceContext.CreateAsync(fixture.ConnectionString, logProvider);

        // Step 1: Create condominio (triggers magic link email)
        var createResult = await context.CreateHandler.HandleAsync(new CreateCondominioCommand(
            context.OperatorUserId,
            "Residencial Log Sanitization",
            "45.723.174/0001-10",
            null, null, null, null, null, null, null, null,
            new DateOnly(2026, 4, 1),
            "Maioria simples",
            "Carlos da Silva",
            "529.982.247-25",
            new DateOnly(2026, 4, 2),
            "Ana Log",
            SensitiveSindicoEmail,
            "+5511999880001"),
            CancellationToken.None);

        Assert.True(createResult.IsSuccess);

        // Extract the raw token from the in-memory FakeEmailSender (not from logs)
        var fakeSender = context.ServiceProvider.GetRequiredService<FakeEmailSender>();
        var email = Assert.Single(fakeSender.SentMessages);
        var rawToken = ExtractTokenFromEmailBody(email.HtmlBody);
        Assert.False(string.IsNullOrWhiteSpace(rawToken));

        // Step 2: Consume the magic link
        var setupResult = await context.SetupHandler.HandleAsync(
            new PasswordSetupCommand(rawToken, SensitivePassword, "127.0.0.1"),
            CancellationToken.None);
        Assert.True(setupResult.IsSuccess);

        // Assert: no sensitive data in any captured log
        var allLogMessages = logProvider.GetAllMessages();

        AssertNotInLogs(allLogMessages, rawToken, "raw magic link token");
        AssertNotInLogs(allLogMessages, SensitivePassword, "password");
        AssertNotInLogs(allLogMessages, SensitiveCpf, "full CPF digits");
        AssertNotInLogs(allLogMessages, SensitiveCnpj, "full CNPJ digits");
        AssertNotInLogs(allLogMessages, SensitiveSindicoEmail, "email in clear text");
    }

    private static void AssertNotInLogs(IReadOnlyList<string> messages, string sensitiveValue, string label)
    {
        var matches = messages.Where(m => m.Contains(sensitiveValue, StringComparison.Ordinal)).ToList();

        if (matches.Count > 0)
        {
            throw new Xunit.Sdk.XunitException(
                $"Log sanitization failure: {label} '{sensitiveValue}' found in {matches.Count} log message(s):\n" +
                string.Join("\n", matches.Take(3).Select(m => $"  > {m}")));
        }
    }

    private static string ExtractTokenFromEmailBody(string htmlBody)
    {
        var match = Regex.Match(htmlBody, @"password-setup\?token=([A-Za-z0-9%_\-]+)", RegexOptions.IgnoreCase);
        return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : string.Empty;
    }

    // ─── test infrastructure ───────────────────���──────────────────────────────

    private sealed class TestServiceContext : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly AsyncServiceScope _scope;
        private readonly string? _previousEnv;

        public IServiceProvider ServiceProvider => _scope.ServiceProvider;
        public Guid OperatorUserId { get; private init; }
        public ICommandHandler<CreateCondominioCommand, CreateCondominioResult> CreateHandler { get; private init; } = null!;
        public ICommandHandler<PasswordSetupCommand, PasswordSetupResult> SetupHandler { get; private init; } = null!;

        private TestServiceContext(ServiceProvider sp, AsyncServiceScope scope, string? previousEnv, Guid operatorId)
        {
            _serviceProvider = sp;
            _scope = scope;
            _previousEnv = previousEnv;
            OperatorUserId = operatorId;
            CreateHandler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateCondominioCommand, CreateCondominioResult>>();
            SetupHandler = scope.ServiceProvider.GetRequiredService<ICommandHandler<PasswordSetupCommand, PasswordSetupResult>>();
        }

        public static async Task<TestServiceContext> CreateAsync(string connectionString, ILoggerProvider logProvider)
        {
            var previousEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

            var config = new ConfigurationBuilder()
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
            services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddProvider(logProvider);
            });
            services.AddInfrastructure(config);
            services.AddPortaBoxModuleGestao(config);

            var sp = services.BuildServiceProvider();
            var scope = sp.CreateAsyncScope();
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

            if (!await roleManager.RoleExistsAsync(IdentityRoles.Operator))
            {
                await roleManager.CreateAsync(new AppRole
                {
                    Id = Guid.NewGuid(),
                    Name = IdentityRoles.Operator,
                    NormalizedName = IdentityRoles.Operator.ToUpperInvariant()
                });
            }

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var operatorEmail = $"operator-sanitize-{Guid.NewGuid():N}@portabox.test";
            var operatorUser = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = operatorEmail,
                NormalizedUserName = operatorEmail.ToUpperInvariant(),
                Email = operatorEmail,
                NormalizedEmail = operatorEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            };
            await userManager.CreateAsync(operatorUser, "PortaBox123!");

            return new TestServiceContext(sp, scope, previousEnv, operatorUser.Id);
        }

        public async ValueTask DisposeAsync()
        {
            await _scope.DisposeAsync();
            await _serviceProvider.DisposeAsync();
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _previousEnv);
        }
    }

    private sealed class CaptureLoggerProvider : ILoggerProvider
    {
        private readonly List<string> _messages = [];
        private readonly object _lock = new();

        public IReadOnlyList<string> GetAllMessages()
        {
            lock (_lock) { return [.. _messages]; }
        }

        public ILogger CreateLogger(string categoryName) => new CaptureLogger(_messages, _lock);

        public void Dispose() { }

        private sealed class CaptureLogger(List<string> messages, object lockObj) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                var message = formatter(state, exception);
                if (!string.IsNullOrEmpty(message))
                {
                    lock (lockObj) { messages.Add(message); }
                }
            }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new();
                public void Dispose() { }
            }
        }
    }
}
