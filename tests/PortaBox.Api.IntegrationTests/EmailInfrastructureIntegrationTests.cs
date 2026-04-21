using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Application.Abstractions.Email;
using PortaBox.Application.Abstractions.Observability;
using PortaBox.Infrastructure.Email;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Api.IntegrationTests;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class EmailInfrastructureIntegrationTests(
    PostgresDatabaseFixture postgresFixture,
    MailHogFixture mailHogFixture) : IClassFixture<MailHogFixture>
{
    [Fact]
    public async Task SmtpEmailSender_ShouldDeliverMessageToMailHogApi()
    {
        await postgresFixture.ResetAsync();

        await using var dbContext = BuildDbContext();
        var sender = CreateSender(dbContext);
        var recipient = $"sindico.{Guid.NewGuid():N}@condominio.test";
        var subject = $"Magic Link {Guid.NewGuid():N}";

        await sender.SendAsync(new EmailMessage(
            recipient,
            subject,
            "<p>Defina sua senha</p>",
            "Defina sua senha"));

        var payload = await mailHogFixture.GetMessagesPayloadAsync();

        Assert.Contains(subject, payload, StringComparison.Ordinal);
        Assert.Contains(recipient, payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SmtpEmailSender_ShouldPersistEmailOutboxEntryAfterPersistentNetworkFailure()
    {
        await postgresFixture.ResetAsync();

        var unavailablePort = ReservePortAndRelease();
        await using var dbContext = BuildDbContext();
        var options = Microsoft.Extensions.Options.Options.Create(new EmailOptions
        {
            Host = "127.0.0.1",
            Port = unavailablePort,
            FromAddress = "no-reply@portabox.dev",
            UseStartTls = false
        });
        var transport = new MailKitEmailTransport(options);
        var dispatcher = new SmtpEmailDispatcher(transport, NullLogger<SmtpEmailDispatcher>.Instance);
        var sender = new SmtpEmailSender(
            dispatcher,
            dbContext,
            NullLogger<SmtpEmailSender>.Instance,
            new NoOpGestaoMetrics(),
            TimeProvider.System);

        await sender.SendAsync(new EmailMessage(
            $"falha.{Guid.NewGuid():N}@condominio.test",
            "Falha SMTP",
            "<p>Falha</p>",
            "Falha"));

        var entry = await dbContext.EmailOutboxEntries.SingleAsync();
        Assert.True(entry.Attempts >= 3);
        Assert.Null(entry.SentAt);
        Assert.NotNull(entry.LastError);
    }

    [Fact]
    public async Task EmailOutboxProcessor_ShouldMarkPendingEntryAsSentAfterSuccessfulRetry()
    {
        await postgresFixture.ResetAsync();

        var recipient = $"retry.{Guid.NewGuid():N}@condominio.test";
        var subject = $"Retry success {Guid.NewGuid():N}";

        await using (var seedContext = BuildDbContext())
        {
            seedContext.EmailOutboxEntries.Add(EmailOutboxEntry.Create(
                recipient,
                subject,
                "<p>Pendente</p>",
                "Pendente",
                attempts: 3,
                nextAttemptAt: TimeProvider.System.GetUtcNow().AddMinutes(-1),
                lastError: "SMTP timeout"));

            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = BuildDbContext();
        var processor = new EmailOutboxProcessor(
            dbContext,
            CreateDispatcher(),
            NullLogger<EmailOutboxProcessor>.Instance,
            new NoOpGestaoMetrics(),
            TimeProvider.System);

        var processed = await processor.ProcessPendingAsync();
        var entry = await dbContext.EmailOutboxEntries.SingleAsync();
        var payload = await mailHogFixture.GetMessagesPayloadAsync();

        Assert.Equal(1, processed);
        Assert.NotNull(entry.SentAt);
        Assert.Contains(subject, payload, StringComparison.Ordinal);
        Assert.Contains(recipient, payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SmtpEmailSender_ShouldLogToHashWithoutPlainRecipientAddress()
    {
        await postgresFixture.ResetAsync();

        await using var dbContext = BuildDbContext();
        var sink = new ListLoggerSink();
        var logger = new ListLogger<SmtpEmailSender>(sink);
        var recipient = $"hash.{Guid.NewGuid():N}@condominio.test";
        var sender = new SmtpEmailSender(
            CreateDispatcher(),
            dbContext,
            logger,
            new NoOpGestaoMetrics(),
            TimeProvider.System);

        await sender.SendAsync(new EmailMessage(
            recipient,
            "Log hash",
            "<p>Body</p>",
            "Body"));

        var entry = Assert.Single(sink.Entries);
        Assert.True(entry.Properties.TryGetValue("to_hash", out var hashedAddress));
        Assert.Equal(EmailAddressHasher.Hash(recipient), hashedAddress?.ToString());
        Assert.DoesNotContain(recipient, entry.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Migration_ShouldCreateEmailOutboxTableAndIndex()
    {
        await postgresFixture.ResetAsync();
        await using var connection = await postgresFixture.OpenConnectionAsync();
        await using var command = new Npgsql.NpgsqlCommand(
            """
            SELECT indexdef
            FROM pg_indexes
            WHERE schemaname = 'public'
              AND tablename = 'email_outbox'
              AND indexname = 'idx_email_outbox_sent_at_next_attempt_at';
            """,
            connection);

        var scalar = await command.ExecuteScalarAsync();

        Assert.NotNull(scalar);
        Assert.Contains("(sent_at, next_attempt_at)", scalar!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private AppDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(postgresFixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        return new AppDbContext(options);
    }

    private SmtpEmailSender CreateSender(AppDbContext dbContext)
    {
        return new SmtpEmailSender(
            CreateDispatcher(),
            dbContext,
            NullLogger<SmtpEmailSender>.Instance,
            new NoOpGestaoMetrics(),
            TimeProvider.System);
    }

    private SmtpEmailDispatcher CreateDispatcher()
    {
        var transport = new MailKitEmailTransport(Microsoft.Extensions.Options.Options.Create(new EmailOptions
        {
            Host = mailHogFixture.Hostname,
            Port = mailHogFixture.SmtpMappedPort,
            FromAddress = "no-reply@portabox.dev",
            UseStartTls = false
        }));

        return new SmtpEmailDispatcher(transport, NullLogger<SmtpEmailDispatcher>.Instance);
    }

    private static int ReservePortAndRelease()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed class ListLoggerSink
    {
        public List<ListLogEntry> Entries { get; } = [];
    }

    private sealed record ListLogEntry(LogLevel Level, string Message, IReadOnlyDictionary<string, object?> Properties);

    private sealed class ListLogger<T>(ListLoggerSink sink) : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state is IEnumerable<KeyValuePair<string, object?>> pairs
                ? pairs.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
                : new Dictionary<string, object?>(StringComparer.Ordinal);

            sink.Entries.Add(new ListLogEntry(logLevel, formatter(state, exception), properties));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }

    private sealed class NoOpGestaoMetrics : IGestaoMetrics
    {
        public void IncrementCondominioActivated() { }
        public void IncrementCondominioCreated(string statusOutcome = "success") { }
        public void IncrementMagicLinkConsumed(string purpose) { }
        public void IncrementMagicLinkExpired(string purpose) { }
        public void IncrementMagicLinkIssued(string purpose) { }
        public void RecordEmailSendDuration(TimeSpan duration, string template, string outcome) { }
        public void UpdateDomainEventOutboxPendingCount(long pendingCount) { }
        public void UpdateEmailOutboxAge(double oldestAgeSeconds) { }
    }
}
