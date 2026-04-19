using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PortaBox.Application.Abstractions.Email;
using PortaBox.Application.Abstractions.Observability;
using PortaBox.Infrastructure;
using PortaBox.Infrastructure.Email;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Api.UnitTests;

public sealed class EmailInfrastructureTests
{
    [Fact]
    public void MailKitEmailTransport_ShouldBuildMimeMessageWithFromToSubjectAndBodies()
    {
        var transport = new MailKitEmailTransport(Microsoft.Extensions.Options.Options.Create(new EmailOptions
        {
            FromAddress = "no-reply@portabox.dev"
        }));

        var mimeMessage = transport.BuildMimeMessage(new EmailMessage(
            "sindico@condominio.test",
            "Defina sua senha",
            "<p>HTML body</p>",
            "Text body"));

        Assert.Equal("no-reply@portabox.dev", mimeMessage.From.Mailboxes.Single().Address);
        Assert.Equal("sindico@condominio.test", mimeMessage.To.Mailboxes.Single().Address);
        Assert.Equal("Defina sua senha", mimeMessage.Subject);

        var multipart = Assert.IsType<MimeKit.MultipartAlternative>(mimeMessage.Body);

        Assert.Equal("<p>HTML body</p>", multipart.OfType<MimeKit.TextPart>().Single(part => part.IsHtml).Text);
        Assert.Equal("Text body", multipart.OfType<MimeKit.TextPart>().Single(part => part.IsPlain).Text);
    }

    [Fact]
    public async Task SmtpEmailSender_ShouldRetryThreeTimesBeforePersistingToOutbox()
    {
        var transport = new ThrowingEmailTransport();
        await using var dbContext = BuildDbContext();
        var dispatcher = new SmtpEmailDispatcher(transport, NullLogger<SmtpEmailDispatcher>.Instance);
        var sender = new SmtpEmailSender(
            dispatcher,
            dbContext,
            NullLogger<SmtpEmailSender>.Instance,
            new NoOpGestaoMetrics(),
            TimeProvider.System);
        var message = new EmailMessage(
            "sindico@condominio.test",
            "Retry test",
            "<p>Body</p>",
            "Body");

        await sender.SendAsync(message);

        var outboxEntry = await dbContext.EmailOutboxEntries.SingleAsync();
        Assert.Equal(3, transport.Attempts);
        Assert.Equal(3, outboxEntry.Attempts);
        Assert.Equal("sindico@condominio.test", outboxEntry.ToAddress);
        Assert.Equal("Retry test", outboxEntry.Subject);
        Assert.NotNull(outboxEntry.LastError);
    }

    [Fact]
    public void EmbeddedResourceEmailTemplateRenderer_ShouldReplaceAllMagicLinkTokens()
    {
        var renderer = new EmbeddedResourceEmailTemplateRenderer();

        var template = renderer.Render(
            "MagicLinkPasswordSetup",
            new Dictionary<string, string>
            {
                ["nome"] = "Maria",
                ["nome_condominio"] = "Residencial Atlântico",
                ["link"] = "https://app.portabox.test/definir-senha?token=abc"
            });

        Assert.Contains("Maria", template.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Residencial Atlântico", template.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("https://app.portabox.test/definir-senha?token=abc", template.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Maria", template.TextBody, StringComparison.Ordinal);
        Assert.Contains("Residencial Atlântico", template.TextBody, StringComparison.Ordinal);
        Assert.Contains("https://app.portabox.test/definir-senha?token=abc", template.TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FakeEmailSender_ShouldAccumulateMessagesInMemory()
    {
        var sender = new FakeEmailSender();
        var first = new EmailMessage("one@condominio.test", "Primeiro", "<p>1</p>", "1");
        var second = new EmailMessage("two@condominio.test", "Segundo", "<p>2</p>", "2");

        await sender.SendAsync(first);
        await sender.SendAsync(second);

        Assert.Collection(
            sender.SentMessages,
            message => Assert.Equal(first, message),
            message => Assert.Equal(second, message));
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterFakeEmailSenderInTestingEnvironment()
    {
        var previousEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

            var provider = BuildServiceProvider(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=localhost;Port=5432;Database=portabox_email_testing;Username=postgres;Password=postgres",
                ["Email:Provider"] = "Smtp",
                ["Email:Host"] = "localhost",
                ["Email:Port"] = "1025"
            });

            using var scope = provider.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            Assert.IsType<FakeEmailSender>(sender);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", previousEnvironment);
        }
    }

    private static AppDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private static ServiceProvider BuildServiceProvider(Dictionary<string, string?> values)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }

    private sealed class ThrowingEmailTransport : IEmailTransport
    {
        public int Attempts { get; private set; }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Attempts++;
            throw new InvalidOperationException("Simulated SMTP failure.");
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
