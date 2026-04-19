using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Application.Abstractions.MagicLinks;
using PortaBox.Application.Abstractions.Observability;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.MagicLinks;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Api.IntegrationTests;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class MagicLinkIntegrationTests(PostgresDatabaseFixture fixture)
{
    [Fact]
    public async Task IssueAndConsume_ShouldMarkConsumedAt()
    {
        await fixture.ResetAsync();

        await using var dbContext = BuildContext();
        var user = await CreateUserAsync(dbContext, "magic-link-consume");
        var service = CreateService(dbContext, TimeProvider.System);

        var issued = await service.IssueAsync(user.Id, MagicLinkPurpose.PasswordSetup);
        var consumed = await service.ValidateAndConsumeAsync(issued.RawToken!, MagicLinkPurpose.PasswordSetup);
        dbContext.ChangeTracker.Clear();
        var persisted = await dbContext.MagicLinks.AsNoTracking().SingleAsync();

        Assert.True(issued.IsSuccess);
        Assert.True(consumed.IsSuccess);
        Assert.NotNull(persisted.ConsumedAt);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_ShouldFailOnSecondConsumption()
    {
        await fixture.ResetAsync();

        await using var dbContext = BuildContext();
        var user = await CreateUserAsync(dbContext, "magic-link-second-consume");
        var service = CreateService(dbContext, TimeProvider.System);

        var issued = await service.IssueAsync(user.Id, MagicLinkPurpose.PasswordSetup);

        Assert.True((await service.ValidateAndConsumeAsync(issued.RawToken!, MagicLinkPurpose.PasswordSetup)).IsSuccess);

        var secondAttempt = await service.ValidateAndConsumeAsync(issued.RawToken!, MagicLinkPurpose.PasswordSetup);

        Assert.False(secondAttempt.IsSuccess);
        Assert.Equal(MagicLinkFailureReason.AlreadyConsumed, secondAttempt.FailureReason);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_ShouldFailForExpiredLink()
    {
        await fixture.ResetAsync();

        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 4, 18, 12, 0, 0, TimeSpan.Zero));
        await using var dbContext = BuildContext();
        var user = await CreateUserAsync(dbContext, "magic-link-expired");
        var service = CreateService(dbContext, timeProvider);

        var issued = await service.IssueAsync(user.Id, MagicLinkPurpose.PasswordSetup);
        timeProvider.Advance(TimeSpan.FromDays(4));

        var result = await service.ValidateAndConsumeAsync(issued.RawToken!, MagicLinkPurpose.PasswordSetup);

        Assert.False(result.IsSuccess);
        Assert.Equal(MagicLinkFailureReason.Expired, result.FailureReason);
    }

    [Fact]
    public async Task InvalidatePendingAsync_ShouldUpdateAllPendingLinksInSingleUpdate()
    {
        await fixture.ResetAsync();

        var sqlStatements = new List<string>();
        await using var dbContext = BuildContext(sql => sqlStatements.Add(sql));
        var user = await CreateUserAsync(dbContext, "magic-link-invalidate-many");
        var now = new DateTimeOffset(2026, 4, 18, 12, 0, 0, TimeSpan.Zero);

        dbContext.MagicLinks.AddRange(
            MagicLink.Create(Guid.NewGuid(), user.Id, MagicLinkPurpose.PasswordSetup, ComputeHash("token-a"), now, now.AddHours(2)),
            MagicLink.Create(Guid.NewGuid(), user.Id, MagicLinkPurpose.PasswordSetup, ComputeHash("token-b"), now, now.AddHours(2)),
            MagicLink.Create(Guid.NewGuid(), user.Id, MagicLinkPurpose.PasswordSetup, ComputeHash("token-c"), now, now.AddHours(2)));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, new MutableTimeProvider(now));

        await service.InvalidatePendingAsync(user.Id, MagicLinkPurpose.PasswordSetup);

        var invalidatedCount = await dbContext.MagicLinks.CountAsync(magicLink => magicLink.InvalidatedAt != null);
        var updateStatements = sqlStatements.Count(statement => statement.Contains("UPDATE", StringComparison.OrdinalIgnoreCase) && statement.Contains("magic_link", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(3, invalidatedCount);
        Assert.Equal(1, updateStatements);
    }

    [Fact]
    public async Task Reissue_ShouldInvalidateOldLinkAndAllowOnlyNewestConsumption()
    {
        await fixture.ResetAsync();

        await using var dbContext = BuildContext();
        var user = await CreateUserAsync(dbContext, "magic-link-reissue");
        var service = CreateService(dbContext, TimeProvider.System);

        var firstIssue = await service.IssueAsync(user.Id, MagicLinkPurpose.PasswordSetup);
        var secondIssue = await service.IssueAsync(user.Id, MagicLinkPurpose.PasswordSetup);

        var firstConsume = await service.ValidateAndConsumeAsync(firstIssue.RawToken!, MagicLinkPurpose.PasswordSetup);
        var secondConsume = await service.ValidateAndConsumeAsync(secondIssue.RawToken!, MagicLinkPurpose.PasswordSetup);

        Assert.False(firstConsume.IsSuccess);
        Assert.Equal(MagicLinkFailureReason.Invalidated, firstConsume.FailureReason);
        Assert.True(secondConsume.IsSuccess);
    }

    [Fact]
    public async Task Migration_ShouldCreateMagicLinkTableAndIndexes()
    {
        await fixture.ResetAsync();
        await using var connection = await fixture.OpenConnectionAsync();

        await using var tableCommand = new NpgsqlCommand(
            """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_name = 'magic_link';
            """,
            connection);

        await using var indexCommand = new NpgsqlCommand(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'public'
              AND tablename = 'magic_link'
              AND indexname IN ('idx_magic_link_token_hash_unique', 'idx_magic_link_user_purpose_open');
            """,
            connection);

        var tableCount = (long)(await tableCommand.ExecuteScalarAsync() ?? 0L);
        var indexes = new List<string>();
        await using (var reader = await indexCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                indexes.Add(reader.GetString(0));
            }
        }

        Assert.Equal(1L, tableCount);
        Assert.Contains("idx_magic_link_token_hash_unique", indexes);
        Assert.Contains("idx_magic_link_user_purpose_open", indexes);
    }

    private AppDbContext BuildContext(Action<string>? log = null)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention();

        if (log is not null)
        {
            optionsBuilder.LogTo(log, LogLevel.Information);
        }

        return new AppDbContext(optionsBuilder.Options);
    }

    private static MagicLinkService CreateService(AppDbContext dbContext, TimeProvider timeProvider)
    {
        return new MagicLinkService(
            dbContext,
            Microsoft.Extensions.Options.Options.Create(new MagicLinkOptions()),
            NullLogger<MagicLinkService>.Instance,
            new NoOpGestaoMetrics(),
            timeProvider);
    }

    private static async Task<AppUser> CreateUserAsync(AppDbContext dbContext, string slug)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{slug}@portabox.test",
            NormalizedUserName = $"{slug}@PORTABOX.TEST",
            Email = $"{slug}@portabox.test",
            NormalizedEmail = $"{slug}@PORTABOX.TEST",
            EmailConfirmed = true
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private static string ComputeHash(string rawToken)
    {
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
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
