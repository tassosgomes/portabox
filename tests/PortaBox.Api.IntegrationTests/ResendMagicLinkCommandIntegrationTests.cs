using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Application.Abstractions.MagicLinks;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Infrastructure;
using PortaBox.Infrastructure.Email;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao;
using PortaBox.Modules.Gestao.Application.Commands.CreateCondominio;
using PortaBox.Modules.Gestao.Application.Commands.ResendMagicLink;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Api.IntegrationTests;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class ResendMagicLinkCommandIntegrationTests(PostgresDatabaseFixture fixture)
{
    [Fact]
    public async Task HandleAsync_TwoRequests_ShouldInvalidatePreviousTokenAndKeepNewestConsumable()
    {
        await fixture.ResetAsync();
        await using var context = await IntegrationContext.CreateAsync(fixture.ConnectionString);
        var created = await context.CreateTenantAsync();

        var firstResend = await context.ResendHandler.HandleAsync(
            new ResendMagicLinkCommand(created.CondominioId, created.SindicoUserId, context.OperatorUser.Id),
            CancellationToken.None);

        var secondResend = await context.ResendHandler.HandleAsync(
            new ResendMagicLinkCommand(created.CondominioId, created.SindicoUserId, context.OperatorUser.Id),
            CancellationToken.None);

        Assert.True(firstResend.IsSuccess);
        Assert.True(secondResend.IsSuccess);

        var messages = context.ServiceProvider.GetRequiredService<FakeEmailSender>().SentMessages.ToArray();
        Assert.Equal(3, messages.Length);

        var firstResentToken = ExtractToken(messages[1].HtmlBody);
        var secondResentToken = ExtractToken(messages[2].HtmlBody);
        var magicLinkService = context.ServiceProvider.GetRequiredService<IMagicLinkService>();

        var firstConsume = await magicLinkService.ValidateAndConsumeAsync(firstResentToken, MagicLinkPurpose.PasswordSetup);
        var secondConsume = await magicLinkService.ValidateAndConsumeAsync(secondResentToken, MagicLinkPurpose.PasswordSetup);

        Assert.False(firstConsume.IsSuccess);
        Assert.Equal(MagicLinkFailureReason.Invalidated, firstConsume.FailureReason);
        Assert.True(secondConsume.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_SixthAttemptWithinWindow_ShouldReturnRateLimitedWithoutSendingEmail()
    {
        await fixture.ResetAsync();
        await using var context = await IntegrationContext.CreateAsync(fixture.ConnectionString);
        var created = await context.CreateTenantAsync();
        var fakeSender = context.ServiceProvider.GetRequiredService<FakeEmailSender>();

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var resend = await context.ResendHandler.HandleAsync(
                new ResendMagicLinkCommand(created.CondominioId, created.SindicoUserId, context.OperatorUser.Id),
                CancellationToken.None);

            Assert.True(resend.IsSuccess);
        }

        var emailsBeforeRateLimit = fakeSender.SentMessages.Count;
        var latestTokenBeforeRateLimit = ExtractToken(fakeSender.SentMessages.Last().HtmlBody);
        var limited = await context.ResendHandler.HandleAsync(
            new ResendMagicLinkCommand(created.CondominioId, created.SindicoUserId, context.OperatorUser.Id),
            CancellationToken.None);

        var consumeAfterRateLimit = await context.ServiceProvider
            .GetRequiredService<IMagicLinkService>()
            .ValidateAndConsumeAsync(latestTokenBeforeRateLimit, MagicLinkPurpose.PasswordSetup);

        Assert.False(limited.IsSuccess);
        Assert.Equal(ResendMagicLinkErrors.RateLimited, limited.Error);
        Assert.Equal(emailsBeforeRateLimit, fakeSender.SentMessages.Count);
        Assert.True(consumeAfterRateLimit.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_Success_ShouldPersistMagicLinkResentAuditEntry()
    {
        await fixture.ResetAsync();
        await using var context = await IntegrationContext.CreateAsync(fixture.ConnectionString);
        var created = await context.CreateTenantAsync();

        var firstResend = await context.ResendHandler.HandleAsync(
            new ResendMagicLinkCommand(created.CondominioId, created.SindicoUserId, context.OperatorUser.Id),
            CancellationToken.None);
        var secondResend = await context.ResendHandler.HandleAsync(
            new ResendMagicLinkCommand(created.CondominioId, created.SindicoUserId, context.OperatorUser.Id),
            CancellationToken.None);

        Assert.True(firstResend.IsSuccess);
        Assert.True(secondResend.IsSuccess);

        var resendAuditEntries = await context.DbContext.TenantAuditEntries
            .Where(entry => entry.TenantId == created.CondominioId)
            .Where(entry => entry.EventKind == TenantAuditEventKind.MagicLinkResent)
            .ToListAsync();

        Assert.Equal(2, resendAuditEntries.Count);
        Assert.All(resendAuditEntries, entry => Assert.Equal(context.OperatorUser.Id, entry.PerformedByUserId));
    }

    [Fact]
    public async Task HandleAsync_SindicoWithPassword_ShouldRejectBeforeIssuingNewToken()
    {
        await fixture.ResetAsync();
        await using var context = await IntegrationContext.CreateAsync(fixture.ConnectionString);
        var created = await context.CreateTenantAsync();
        var fakeSender = context.ServiceProvider.GetRequiredService<FakeEmailSender>();

        var user = await context.DbContext.Users.SingleAsync(current => current.Id == created.SindicoUserId);
        user.PasswordHash = "already-set";
        await context.DbContext.SaveChangesAsync();

        var magicLinkCountBefore = await context.DbContext.MagicLinks.CountAsync(current => current.UserId == created.SindicoUserId);
        var emailCountBefore = fakeSender.SentMessages.Count;

        var result = await context.ResendHandler.HandleAsync(
            new ResendMagicLinkCommand(created.CondominioId, created.SindicoUserId, context.OperatorUser.Id),
            CancellationToken.None);

        var magicLinkCountAfter = await context.DbContext.MagicLinks.CountAsync(current => current.UserId == created.SindicoUserId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResendMagicLinkErrors.AlreadyHasPassword, result.Error);
        Assert.Equal(magicLinkCountBefore, magicLinkCountAfter);
        Assert.Equal(emailCountBefore, fakeSender.SentMessages.Count);
    }

    private static string ExtractToken(string htmlBody)
    {
        const string marker = "token=";
        var markerIndex = htmlBody.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0);

        var tokenStart = markerIndex + marker.Length;
        var tokenEnd = htmlBody.IndexOfAny(['"', '\'', '<', ' ', '&'], tokenStart);
        if (tokenEnd < 0)
        {
            tokenEnd = htmlBody.Length;
        }

        return Uri.UnescapeDataString(htmlBody[tokenStart..tokenEnd]);
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
            ICommandHandler<ResendMagicLinkCommand, ResendMagicLinkResult> resendHandler,
            AppUser operatorUser,
            string? previousEnvironment)
        {
            _serviceProvider = serviceProvider;
            _scope = scope;
            DbContext = dbContext;
            CreateHandler = createHandler;
            ResendHandler = resendHandler;
            OperatorUser = operatorUser;
            _previousEnvironment = previousEnvironment;
        }

        public IServiceProvider ServiceProvider => _scope.ServiceProvider;

        public AppDbContext DbContext { get; }

        public ICommandHandler<CreateCondominioCommand, CreateCondominioResult> CreateHandler { get; }

        public ICommandHandler<ResendMagicLinkCommand, ResendMagicLinkResult> ResendHandler { get; }

        public AppUser OperatorUser { get; }

        public async Task<CreateCondominioResult> CreateTenantAsync()
        {
            var result = await CreateHandler.HandleAsync(
                new CreateCondominioCommand(
                    OperatorUser.Id,
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
                    scope.ServiceProvider.GetRequiredService<ICommandHandler<ResendMagicLinkCommand, ResendMagicLinkResult>>(),
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
