using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Database;
using Neba.Api.Discord;
using Neba.Api.Email;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Legacy.Seasons.Complete;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;

using Npgsql;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Legacy.Seasons.Complete;

// This job's whole job is "resolve by date-range match, complete, then schedule the eight award
// jobs an hour out" - covered separately from the endpoint test (which only proves this job gets
// enqueued) and from the award jobs themselves (which do the actual ranking/assignment work once
// scheduled here). Mirrors CompleteTournamentSyncJobTests's shape; the legacy Season lookup uses
// Postgres as a stand-in for neba-fwk's MSSQL, same as GenerateSeasonStatsJobTests.
[IntegrationTest]
[Component("Legacy")]
[Collection<AppDbContextFixture>]
public sealed class CompleteSeasonSyncJobTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private NpgsqlConnection _legacyConnection = null!;
    private ServiceProvider _serviceProvider = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();

        var services = new ServiceCollection();
        services.AddFusionCache().WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromHours(1));
        _serviceProvider = services.BuildServiceProvider();

        _legacyConnection = new NpgsqlConnection(fixture.ConnectionString);
        await _legacyConnection.OpenAsync();

        await using var create = _legacyConnection.CreateCommand();
        create.CommandText = """
            CREATE TEMP TABLE Season (
                Id integer PRIMARY KEY,
                Start timestamp NOT NULL,
                "end" timestamp NOT NULL
            );
            """;
        await create.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _legacyConnection.DisposeAsync();
        await _serviceProvider.DisposeAsync();
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task InsertLegacySeasonAsync(int legacySeasonId, DateTime start, DateTime end)
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = """INSERT INTO Season (Id, Start, "end") VALUES (@Id, @Start, @End)""";
        insert.Parameters.AddWithValue("@Id", legacySeasonId);
        insert.Parameters.AddWithValue("@Start", start);
        insert.Parameters.AddWithValue("@End", end);
        await insert.ExecuteNonQueryAsync();
    }

    private async Task<Season> CreateSeasonAsync(DateOnly startDate, DateOnly endDate, bool complete, CancellationToken ct)
    {
        var season = SeasonFactory.Create(startDate: startDate, endDate: endDate, complete: complete);
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        return season;
    }

    private CompleteSeasonSyncJob CreateJob(
        IBackgroundJobClient jobs,
        Mock<IEmailSender>? emailSender = null,
        Mock<IDiscordNotifier>? discordNotifier = null,
        FakeLogger<CompleteSeasonSyncJob>? logger = null) =>
        new(
            _dbContext,
            _legacyConnection,
            jobs,
            _serviceProvider.GetRequiredService<IFusionCache>(),
            (emailSender ?? new Mock<IEmailSender>(MockBehavior.Strict)).Object,
            (discordNotifier ?? new Mock<IDiscordNotifier>(MockBehavior.Strict)).Object,
            logger ?? new FakeLogger<CompleteSeasonSyncJob>());

    private static (Mock<IBackgroundJobClient> Mock, Func<IReadOnlyList<(Job Job, IState State)>> CapturedJobs) CreateJobsMock()
    {
        var captured = new List<(Job Job, IState State)>();
        var mock = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        mock.Setup(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, state) => captured.Add((job, state)))
            .Returns("job-1");

        return (mock, () => captured);
    }

    private static readonly Type[] ExpectedAwardJobTypes =
    [
        typeof(AssignBowlerOfTheYearAwardJob),
        typeof(AssignWomanOfTheYearAwardJob),
        typeof(AssignSeniorBowlerOfTheYearAwardJob),
        typeof(AssignSuperSeniorBowlerOfTheYearAwardJob),
        typeof(AssignRookieBowlerOfTheYearAwardJob),
        typeof(AssignYouthBowlerOfTheYearAwardJob),
        typeof(AssignHighAverageAwardJob),
        typeof(AssignHighBlockAwardJob)
    ];

    [Fact(DisplayName = "SyncAsync should complete the season, save, and schedule all eight award jobs an hour out when the season is found and not yet complete")]
    public async Task SyncAsync_ShouldCompleteSaveAndScheduleAwardJobs_WhenSeasonFoundAndNotComplete()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 12, 31);
        var season = await CreateSeasonAsync(startDate, endDate, complete: false, ct);
        await InsertLegacySeasonAsync(42, startDate.ToDateTime(TimeOnly.MinValue), endDate.ToDateTime(TimeOnly.MinValue));

        var (jobsMock, capturedJobs) = CreateJobsMock();
        var job = CreateJob(jobsMock.Object);

        // Act
        await job.SyncAsync(42, ct);

        // Assert
        var updated = await _dbContext.Seasons.SingleAsync(s => s.Id == season.Id, ct);
        updated.Complete.ShouldBeTrue();

        var chained = capturedJobs();
        chained.Count.ShouldBe(ExpectedAwardJobTypes.Length);

        foreach (var expectedType in ExpectedAwardJobTypes)
        {
            var scheduled = chained.Single(c => c.Job.Type == expectedType);
            scheduled.Job.Method.Name.ShouldBe("AssignAsync");
            scheduled.Job.Args[0].ShouldBe(season.Id);
            var scheduledState = scheduled.State.ShouldBeOfType<ScheduledState>();
            scheduledState.EnqueueAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(59));
        }
    }

    [Fact(DisplayName = "SyncAsync should evict the seasons list cache tag when the season is completed for the first time")]
    public async Task SyncAsync_ShouldEvictSeasonsListCacheTag_WhenSeasonCompletedForTheFirstTime()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 12, 31);
        await CreateSeasonAsync(startDate, endDate, complete: false, ct);
        await InsertLegacySeasonAsync(42, startDate.ToDateTime(TimeOnly.MinValue), endDate.ToDateTime(TimeOnly.MinValue));

        var (jobsMock, _) = CreateJobsMock();

        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string cacheKey = "complete-season-cache-test";
        await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("stale-cached-value"), tags: ["neba:seasons"], token: ct);

        var job = CreateJob(jobsMock.Object);

        // Act
        await job.SyncAsync(42, ct);

        // Assert - a stale cached value would be returned by GetOrSetAsync instead of invoking the factory.
        var valueAfterSync = await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("fresh-value"), token: ct);
        valueAfterSync.ShouldBe("fresh-value");
    }

    [Fact(DisplayName = "SyncAsync should not evict the seasons list cache tag when the season was already complete")]
    public async Task SyncAsync_ShouldNotEvictSeasonsListCacheTag_WhenSeasonAlreadyComplete()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 12, 31);
        await CreateSeasonAsync(startDate, endDate, complete: true, ct);
        await InsertLegacySeasonAsync(42, startDate.ToDateTime(TimeOnly.MinValue), endDate.ToDateTime(TimeOnly.MinValue));

        var (jobsMock, _) = CreateJobsMock();

        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string cacheKey = "complete-season-idempotent-cache-test";
        await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("stale-cached-value"), tags: ["neba:seasons"], token: ct);

        var job = CreateJob(jobsMock.Object);

        // Act
        await job.SyncAsync(42, ct);

        // Assert - nothing changed on the AlreadyComplete branch, so the stale entry survives.
        var valueAfterSync = await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("fresh-value"), token: ct);
        valueAfterSync.ShouldBe("stale-cached-value");
    }

    [Fact(DisplayName = "SyncAsync should still schedule all eight award jobs when the season was already complete")]
    public async Task SyncAsync_ShouldStillScheduleAwardJobs_WhenSeasonAlreadyComplete()
    {
        // Arrange - idempotent re-fire: AlreadyComplete is informational, not fatal.
        var ct = TestContext.Current.CancellationToken;
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 12, 31);
        await CreateSeasonAsync(startDate, endDate, complete: true, ct);
        await InsertLegacySeasonAsync(42, startDate.ToDateTime(TimeOnly.MinValue), endDate.ToDateTime(TimeOnly.MinValue));

        var (jobsMock, capturedJobs) = CreateJobsMock();
        var fakeLogger = new FakeLogger<CompleteSeasonSyncJob>();
        var job = CreateJob(jobsMock.Object, logger: fakeLogger);

        // Act
        await job.SyncAsync(42, ct);

        // Assert
        var chained = capturedJobs();
        chained.Count.ShouldBe(ExpectedAwardJobTypes.Length);
        fakeLogger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Information);
    }

    [Fact(DisplayName = "SyncAsync should not schedule any award job and should send a manual-intervention email and Discord alert when no legacy season is found")]
    public async Task SyncAsync_ShouldNotScheduleAndShouldSendEmail_WhenLegacySeasonNotFound()
    {
        // Arrange - Strict jobs mock with no setup: any Create call would throw, proving nothing was scheduled.
        var ct = TestContext.Current.CancellationToken;
        var jobsMock = new Mock<IBackgroundJobClient>(MockBehavior.Strict);

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        EmailMessage? sentMessage = null;
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => sentMessage = message)
            .Returns(Task.CompletedTask);

        var discordNotifier = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        DiscordAlert? postedAlert = null;
        discordNotifier
            .Setup(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()))
            .Callback<DiscordAlert, CancellationToken>((alert, _) => postedAlert = alert)
            .Returns(Task.CompletedTask);

        var job = CreateJob(jobsMock.Object, emailSender, discordNotifier);

        // Act
        await job.SyncAsync(999, ct);

        // Assert - Strict mock with no Create setup already proves nothing was scheduled (see Arrange comment).
        sentMessage.ShouldNotBeNull();
        sentMessage.To.ShouldBe("website@bowlneba.com");
        sentMessage.HtmlBody.ShouldContain("999");

        // The captured alert's content below already proves NotifyAsync was called.
        postedAlert.ShouldNotBeNull();
        postedAlert.Severity.ShouldBe(DiscordAlertSeverity.Critical);
        postedAlert.Metadata.ShouldNotBeNull();
        postedAlert.Metadata["LegacySeasonId"].ShouldBe("999");
    }

    [Fact(DisplayName = "SyncAsync should not schedule any award job and should send a manual-intervention email and Discord alert when no website season matches the legacy date range")]
    public async Task SyncAsync_ShouldNotScheduleAndShouldSendEmail_WhenNoWebsiteSeasonMatches()
    {
        // Arrange - Strict jobs mock with no setup: any Create call would throw, proving nothing was scheduled.
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacySeasonAsync(42, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        // No CreateSeasonAsync call - no website season exists at all, let alone a matching one.

        var jobsMock = new Mock<IBackgroundJobClient>(MockBehavior.Strict);

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        EmailMessage? sentMessage = null;
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => sentMessage = message)
            .Returns(Task.CompletedTask);

        var discordNotifier = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        DiscordAlert? postedAlert = null;
        discordNotifier
            .Setup(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()))
            .Callback<DiscordAlert, CancellationToken>((alert, _) => postedAlert = alert)
            .Returns(Task.CompletedTask);

        var job = CreateJob(jobsMock.Object, emailSender, discordNotifier);

        // Act
        await job.SyncAsync(42, ct);

        // Assert - Strict mock with no Create setup already proves nothing was scheduled (see Arrange comment).
        sentMessage.ShouldNotBeNull();
        sentMessage.HtmlBody.ShouldContain("42");

        // The captured alert's content below already proves NotifyAsync was called.
        postedAlert.ShouldNotBeNull();
        postedAlert.Severity.ShouldBe(DiscordAlertSeverity.Critical);
        postedAlert.Metadata.ShouldNotBeNull();
        postedAlert.Metadata["LegacySeasonId"].ShouldBe("42");
    }
}