using System.Diagnostics.CodeAnalysis;

using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Database;
using Neba.Api.Discord;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Legacy;
using Neba.Api.Legacy.Sync;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Legacy.Sync;

// Mirrors the file shape of the production source (Legacy/Sync/) so the whole test suite for the
// weekly sync is removed alongside it at sunset. There is no request, validator or route, so the
// endpoint, auth and validator layers of the usual backdoor test plan do not apply.

[UnitTest]
[Component("Legacy")]
public sealed class WeeklyLegacySyncScheduleTests
{
    [Fact(DisplayName = "WeeklyLegacySync should run Mondays at 8 AM")]
    public void Cron_ShouldBeMondayAtEightAm()
    {
        // Arrange / Act / Assert
        WeeklyLegacySync.Cron.ShouldBe("0 8 * * 1");
    }

    [Fact(DisplayName = "WeeklyLegacySync should look back 8 days")]
    public void LookbackDays_ShouldBeEight()
    {
        // Arrange / Act / Assert
        WeeklyLegacySync.LookbackDays.ShouldBe(8);
    }

    [Fact(DisplayName = "LegacyTimeZones.Eastern should be an Eastern zone that observes daylight saving")]
    public void Eastern_ShouldBeEasternZone()
    {
        // Arrange
        var winter = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var summer = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

        // Act
        var winterOffset = LegacyTimeZones.Eastern.GetUtcOffset(winter);
        var summerOffset = LegacyTimeZones.Eastern.GetUtcOffset(summer);

        // Assert
        winterOffset.ShouldBe(TimeSpan.FromHours(-5));
        summerOffset.ShouldBe(TimeSpan.FromHours(-4));
    }
}

[UnitTest]
[Component("Legacy")]
public sealed class WeeklyLegacySyncJobTests
{
    [Fact(DisplayName = "RunAsync should enqueue one job per registered table, each with the same since value")]
    public async Task RunAsync_ShouldEnqueueOneJobPerTable_WithSameSince()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(now);
        var captured = new List<Job>();
        var jobs = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        jobs
            .Setup(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, _) => captured.Add(job))
            .Returns("job-1");

        var job = new WeeklyLegacySyncJob(jobs.Object, clock, new FakeLogger<WeeklyLegacySyncJob>());

        // Act
        await job.RunAsync();

        // Assert
        captured.Count.ShouldBe(WeeklyLegacySync.Tables.Count);
        var bowlerJob = captured.Single(c => c.Type == typeof(BowlerWeeklySyncJob));
        bowlerJob.Method.Name.ShouldBe(nameof(BowlerWeeklySyncJob.SyncAsync));
        bowlerJob.Args[0].ShouldBe(now.UtcDateTime.AddDays(-8));
    }

    [Fact(DisplayName = "RunAsync should log the failure and not throw when a table cannot be enqueued")]
    public async Task RunAsync_ShouldLogAndNotThrow_WhenEnqueueFails()
    {
        // Arrange
        var jobs = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        jobs
            .Setup(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Throws(new InvalidOperationException("storage unavailable"));
        var logger = new FakeLogger<WeeklyLegacySyncJob>();
        var job = new WeeklyLegacySyncJob(jobs.Object, new FakeTimeProvider(), logger);

        // Act
        await job.RunAsync();

        // Assert
        logger.Collector.GetSnapshot().ShouldContain(record =>
            record.Level == LogLevel.Error && record.Exception is InvalidOperationException);
    }

    [Fact(DisplayName = "RunAsync should log how many tables it is syncing")]
    public async Task RunAsync_ShouldLogTableCount()
    {
        // Arrange
        var jobs = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        jobs
            .Setup(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Returns("job-1");
        var logger = new FakeLogger<WeeklyLegacySyncJob>();
        var job = new WeeklyLegacySyncJob(jobs.Object, new FakeTimeProvider(), logger);

        // Act
        await job.RunAsync();

        // Assert
        var record = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Information);
    }
}

[IntegrationTest]
[Component("Legacy")]
[Collection(nameof(LegacyDatabasesTestScope))]
public sealed class BowlerWeeklySyncJobTests(AppDbContextFixture fixture, LegacySqlServerFixture legacyFixture)
    : IClassFixture<AppDbContextFixture>, IClassFixture<LegacySqlServerFixture>, IAsyncLifetime
{
    private static readonly DateTime Since = new(2026, 10, 11, 8, 0, 0, DateTimeKind.Unspecified);

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    // Ownership stays with LegacySqlServerFixture, which disposes it once for the whole run.
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed",
        Justification = "Ownership stays with LegacySqlServerFixture, which disposes it once for the whole run.")]
    private LegacySqlServerDatabase _legacyDatabase = null!;
    private ServiceProvider _serviceProvider = null!;

    private SqlConnection _legacyConnection => _legacyDatabase.Connection;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();

        var services = new ServiceCollection();
        services.AddFusionCache().WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromHours(1));
        _serviceProvider = services.BuildServiceProvider();

        _legacyDatabase = await legacyFixture.GetOrCreateDatabaseAsync(nameof(BowlerWeeklySyncJobTests), CreateSchemaAsync);
        await _legacyDatabase.ResetAsync();
    }

    private static async Task CreateSchemaAsync(SqlConnection connection)
    {
        await using var create = connection.CreateCommand();
        create.CommandText = """
            CREATE TABLE Bowlers (
                Id int PRIMARY KEY,
                FirstName nvarchar(100) NOT NULL,
                MiddleInitial nvarchar(10) NULL,
                LastName nvarchar(100) NOT NULL,
                Suffix nvarchar(10) NULL,
                Gender int NOT NULL,
                DateOfBirth datetime NULL,
                Audit_UpdatedTimestamp datetime NOT NULL
            )
            """;
        await create.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task InsertLegacyBowlerAsync(
        int id,
        DateTime updated,
        string firstName = "David",
        string? middleInitial = null,
        string lastName = "Smith",
        string? suffix = null,
        int gender = 0,
        DateTime? dateOfBirth = null)
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = """
            INSERT INTO Bowlers (Id, FirstName, MiddleInitial, LastName, Suffix, Gender, DateOfBirth, Audit_UpdatedTimestamp)
            VALUES (@Id, @FirstName, @MiddleInitial, @LastName, @Suffix, @Gender, @DateOfBirth, @Updated)
            """;
        insert.Parameters.AddWithValue("@Id", id);
        insert.Parameters.AddWithValue("@FirstName", firstName);
        insert.Parameters.AddWithValue("@MiddleInitial", (object?)middleInitial ?? DBNull.Value);
        insert.Parameters.AddWithValue("@LastName", lastName);
        insert.Parameters.AddWithValue("@Suffix", (object?)suffix ?? DBNull.Value);
        insert.Parameters.AddWithValue("@Gender", gender);
        insert.Parameters.AddWithValue("@DateOfBirth", dateOfBirth is null ? DBNull.Value : dateOfBirth.Value);
        insert.Parameters.AddWithValue("@Updated", updated);
        await insert.ExecuteNonQueryAsync();
    }

    private async Task SeedExistingBowlerAsync(int legacyId, Name? name = null, Gender? gender = null, DateOnly? dateOfBirth = null)
    {
        var bowler = BowlerFactory.Create(name: name, legacyId: legacyId, gender: gender, dateOfBirth: dateOfBirth);
        await _dbContext.Set<Bowler>().AddAsync(bowler, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        _dbContext.ChangeTracker.Clear();
    }

    private BowlerWeeklySyncJob CreateJob(
        FakeLogger<BowlerWeeklySyncJob>? logger = null,
        Mock<IManualInterventionNotifier>? notifier = null) =>
        new(
            _dbContext,
            _legacyConnection,
            _serviceProvider.GetRequiredService<IFusionCache>(),
            (notifier ?? new Mock<IManualInterventionNotifier>(MockBehavior.Strict)).Object,
            logger ?? new FakeLogger<BowlerWeeklySyncJob>());

    [Fact(DisplayName = "SyncAsync should only read bowlers updated at or after the since value")]
    public async Task SyncAsync_ShouldOnlyReadBowlersUpdatedAtOrAfterSince()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyBowlerAsync(1, Since.AddSeconds(-1));
        await InsertLegacyBowlerAsync(2, Since);
        await InsertLegacyBowlerAsync(3, Since.AddDays(1));
        var job = CreateJob();

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        var legacyIds = await _dbContext.Set<Bowler>().Select(b => b.LegacyId).ToListAsync(ct);
        legacyIds.Order().ShouldBe([2, 3]);
    }

    [Fact(DisplayName = "SyncAsync should create a bowler when the legacy id has no website record")]
    public async Task SyncAsync_ShouldCreateBowler_WhenLegacyIdHasNoWebsiteRecord()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyBowlerAsync(
            1,
            Since.AddDays(1),
            firstName: "William \"Bill\"",
            middleInitial: "M",
            lastName: "Smith",
            suffix: "Jr",
            gender: 1,
            dateOfBirth: new DateTime(1990, 6, 15, 0, 0, 0, DateTimeKind.Unspecified));
        var job = CreateJob();

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        var bowler = await _dbContext.Set<Bowler>().SingleAsync(ct);
        bowler.LegacyId.ShouldBe(1);
        bowler.Name.FirstName.ShouldBe("William");
        bowler.Name.Nickname.ShouldBe("Bill");
        bowler.Name.LastName.ShouldBe("Smith");
        bowler.Name.MiddleName.ShouldBe("M");
        bowler.Name.Suffix.ShouldBe(NameSuffix.Jr);
        bowler.Gender.ShouldBe(Gender.Female);
        bowler.DateOfBirth.ShouldBe(new DateOnly(1990, 6, 15));
    }

    [Fact(DisplayName = "SyncAsync should update an existing bowler whose fields differ from the Software")]
    public async Task SyncAsync_ShouldUpdateExistingBowler_WhenFieldsDiffer()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedExistingBowlerAsync(
            1,
            name: NameFactory.Create(firstName: "Original", lastName: "Bowler"),
            gender: Gender.Male,
            dateOfBirth: new DateOnly(1980, 1, 1));
        await InsertLegacyBowlerAsync(1, Since.AddDays(1), firstName: "David", lastName: "Smith", gender: 1);
        var job = CreateJob();

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        var bowler = await _dbContext.Set<Bowler>().SingleAsync(ct);
        bowler.Name.FirstName.ShouldBe("David");
        bowler.Name.LastName.ShouldBe("Smith");
        bowler.Gender.ShouldBe(Gender.Female);
        bowler.DateOfBirth.ShouldBeNull();
    }

    [Fact(DisplayName = "SyncAsync should be idempotent: a second run changes nothing and evicts nothing")]
    public async Task SyncAsync_ShouldBeIdempotent_WhenRunTwice()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyBowlerAsync(1, Since.AddDays(1), firstName: "David", lastName: "Smith", gender: 0);
        var logger = new FakeLogger<BowlerWeeklySyncJob>();
        var job = CreateJob(logger);
        await job.SyncAsync(Since, ct);

        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        await cache.GetOrSetAsync("weekly-sync-test", _ => Task.FromResult("cached"), tags: ["neba:bowlers"], token: ct);
        logger.Collector.Clear();

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        (await _dbContext.Set<Bowler>().CountAsync(ct)).ShouldBe(1);
        var stillCached = await cache.GetOrSetAsync("weekly-sync-test", _ => Task.FromResult("fresh"), token: ct);
        stillCached.ShouldBe("cached");
        logger.Collector.GetSnapshot().Single().Message.ShouldContain("1 already current");
    }

    [Fact(DisplayName = "SyncAsync should evict only the bowlers cache tag when it creates or updates a bowler")]
    public async Task SyncAsync_ShouldEvictBowlersTagOnly_WhenBowlerChanges()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyBowlerAsync(1, Since.AddDays(1));
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        await cache.GetOrSetAsync("weekly-sync-bowlers", _ => Task.FromResult("stale"), tags: ["neba", "neba:bowlers"], token: ct);
        await cache.GetOrSetAsync("weekly-sync-other", _ => Task.FromResult("kept"), tags: ["neba", "neba:documents"], token: ct);
        var job = CreateJob();

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        var bowlers = await cache.GetOrSetAsync("weekly-sync-bowlers", _ => Task.FromResult("fresh"), token: ct);
        var other = await cache.GetOrSetAsync("weekly-sync-other", _ => Task.FromResult("fresh"), token: ct);
        bowlers.ShouldBe("fresh");
        other.ShouldBe("kept");
    }

    [Fact(DisplayName = "SyncAsync should keep the website nickname when the legacy first name has no quoted nickname")]
    public async Task SyncAsync_ShouldKeepNickname_WhenLegacyNameHasNoQuotes()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedExistingBowlerAsync(
            1,
            name: NameFactory.Create(firstName: "William", lastName: "Smith", nickname: "Bill"));
        await InsertLegacyBowlerAsync(1, Since.AddDays(1), firstName: "William", lastName: "Smythe");
        var job = CreateJob();

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        var bowler = await _dbContext.Set<Bowler>().SingleAsync(ct);
        bowler.Name.LastName.ShouldBe("Smythe");
        bowler.Name.Nickname.ShouldBe("Bill");
    }

    [Fact(DisplayName = "SyncAsync should replace the website nickname when the legacy first name has a quoted nickname")]
    public async Task SyncAsync_ShouldReplaceNickname_WhenLegacyNameHasQuotedNickname()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedExistingBowlerAsync(
            1,
            name: NameFactory.Create(firstName: "William", lastName: "Smith", nickname: "Bill"));
        await InsertLegacyBowlerAsync(1, Since.AddDays(1), firstName: "William \"Will\"", lastName: "Smith");
        var job = CreateJob();

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        var bowler = await _dbContext.Set<Bowler>().SingleAsync(ct);
        bowler.Name.Nickname.ShouldBe("Will");
    }

    [Fact(DisplayName = "SyncAsync should save the other bowlers and alert when the database rejects one row")]
    public async Task SyncAsync_ShouldSaveRestAndAlert_WhenDatabaseRejectsOneRow()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        // Passes domain validation but exceeds the bowlers.last_name column length.
        await InsertLegacyBowlerAsync(1, Since.AddDays(1), lastName: new string('x', 100));
        await InsertLegacyBowlerAsync(2, Since.AddDays(1), lastName: "Jones");

        DiscordAlert? alert = null;
        var notifier = new Mock<IManualInterventionNotifier>(MockBehavior.Strict);
        notifier
            .Setup(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()))
            .Callback<DiscordAlert, CancellationToken>((a, _) => alert = a)
            .Returns(Task.CompletedTask);
        var logger = new FakeLogger<BowlerWeeklySyncJob>();
        var job = CreateJob(logger, notifier);

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        (await _dbContext.Set<Bowler>().SingleAsync(ct)).LegacyId.ShouldBe(2);
        logger.Collector.GetSnapshot().ShouldContain(record => record.Level == LogLevel.Error && record.Exception != null);
        alert.ShouldNotBeNull();
        alert.Metadata.ShouldNotBeNull();
        alert.Metadata["LegacyBowlerIds"].ShouldContain("1");
    }

    [Fact(DisplayName = "SyncAsync should skip a bad row, save the rest, and send one alert")]
    public async Task SyncAsync_ShouldSkipBadRowSaveRestAndAlert_WhenOneRowFailsMapping()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyBowlerAsync(1, Since.AddDays(1), lastName: " ");
        await InsertLegacyBowlerAsync(2, Since.AddDays(1), lastName: "Jones");

        DiscordAlert? alert = null;
        var notifier = new Mock<IManualInterventionNotifier>(MockBehavior.Strict);
        notifier
            .Setup(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()))
            .Callback<DiscordAlert, CancellationToken>((a, _) => alert = a)
            .Returns(Task.CompletedTask);
        var logger = new FakeLogger<BowlerWeeklySyncJob>();
        var job = CreateJob(logger, notifier);

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        (await _dbContext.Set<Bowler>().SingleAsync(ct)).LegacyId.ShouldBe(2);
        logger.Collector.GetSnapshot().ShouldContain(record => record.Level == LogLevel.Error);
        alert.ShouldNotBeNull();
        alert.Metadata.ShouldNotBeNull();
        alert.Metadata["LegacyBowlerIds"].ShouldContain("1");
    }

    [Fact(DisplayName = "SyncAsync should save every bowler when more than one batch of changes comes back")]
    public async Task SyncAsync_ShouldSaveEveryBowler_WhenMoreThanOneBatch()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        for (var id = 1; id <= 120; id++)
        {
            await InsertLegacyBowlerAsync(id, Since.AddDays(1), lastName: $"Bowler{id}");
        }

        var job = CreateJob();

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        (await _dbContext.Set<Bowler>().CountAsync(ct)).ShouldBe(120);
    }
}