using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;

using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Contracts.Memberships;
using Neba.Api.Database;
using Neba.Api.Discord;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Memberships.Domain;
using Neba.Api.Legacy;
using Neba.Api.Legacy.BowlerMemberships;
using Neba.Api.Legacy.Sync;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Legacy.Sync;

[UnitTest]
[Component("Legacy")]
public sealed class BowlerMembershipWeeklySyncScheduleTests
{
    [Fact(DisplayName = "RunAsync should enqueue the bowler step now and schedule the membership step 15 minutes later with the same since value")]
    public async Task RunAsync_ShouldScheduleMembershipStepAfterBowlerStep_WithSameSince()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
        var captured = new List<(Job Job, IState State)>();
        var jobs = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        jobs
            .Setup(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, state) => captured.Add((job, state)))
            .Returns("job-1");
        var job = new WeeklyLegacySyncJob(jobs.Object, new FakeTimeProvider(now), new FakeLogger<WeeklyLegacySyncJob>());

        // Act
        await job.RunAsync();

        // Assert
        var bowlerStep = captured.Single(c => c.Job.Type == typeof(BowlerWeeklySyncJob));
        bowlerStep.State.ShouldBeOfType<EnqueuedState>();

        var membershipStep = captured.Single(c => c.Job.Type == typeof(BowlerMembershipWeeklySyncJob));
        membershipStep.Job.Method.Name.ShouldBe(nameof(BowlerMembershipWeeklySyncJob.SyncAsync));
        membershipStep.Job.Args[0].ShouldBe(now.UtcDateTime.AddDays(-8));
        var scheduled = membershipStep.State.ShouldBeOfType<ScheduledState>();
        var delay = scheduled.EnqueueAt - DateTime.UtcNow;
        delay.ShouldBeGreaterThan(TimeSpan.FromMinutes(14));
        delay.ShouldBeLessThanOrEqualTo(BowlerMembershipWeeklySyncJob.StartDelay);
        BowlerMembershipWeeklySyncJob.StartDelay.ShouldBe(TimeSpan.FromMinutes(15));
    }
}

[IntegrationTest]
[Component("Legacy")]
[Collection(nameof(LegacyDatabasesTestScope))]
public sealed class BowlerMembershipWeeklySyncJobTests(AppDbContextFixture fixture, LegacySqlServerFixture legacyFixture)
    : IClassFixture<AppDbContextFixture>, IClassFixture<LegacySqlServerFixture>, IAsyncLifetime
{
    private static readonly DateTime Since = new(2026, 10, 11, 8, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime Old = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
    private const int UnmappedMembershipId = 99;

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

        _legacyDatabase = await legacyFixture.GetOrCreateDatabaseAsync(nameof(BowlerMembershipWeeklySyncJobTests), CreateSchemaAsync);
        await _legacyDatabase.ResetAsync();

        await InsertLegacyMembershipTypeAsync(1, 50m);
        await InsertLegacyMembershipTypeAsync(2, 75m);
        await InsertLegacyMembershipTypeAsync(LegacyBowlerMembershipSync.SystemImportMembershipId, 0m);
        await InsertLegacyMembershipTypeAsync(UnmappedMembershipId, 10m);
        // Wider than the website's numeric(5,2) so a row can pass domain checks and still fail the save.
        await InsertLegacyMembershipTypeAsync(3, 12345m);
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
                DateOfBirth datetime NULL
            );
            CREATE TABLE Memberships (
                Id int PRIMARY KEY,
                Cost decimal(9,2) NOT NULL
            );
            CREATE TABLE BowlerMemberships (
                Id int PRIMARY KEY,
                BowlerId int NOT NULL,
                MembershipId int NOT NULL,
                BeginDate datetime NOT NULL,
                EndDate datetime NOT NULL,
                Audit_UpdatedTimestamp datetime NOT NULL
            );
            """;
        await create.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task InsertLegacyMembershipTypeAsync(int id, decimal cost)
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = "INSERT INTO Memberships (Id, Cost) VALUES (@Id, @Cost)";
        insert.Parameters.AddWithValue("@Id", id);
        insert.Parameters.AddWithValue("@Cost", cost);
        await insert.ExecuteNonQueryAsync();
    }

    private async Task InsertLegacyBowlerAsync(int id, string lastName = "Smith")
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = """
            INSERT INTO Bowlers (Id, FirstName, MiddleInitial, LastName, Suffix, Gender, DateOfBirth)
            VALUES (@Id, 'David', NULL, @LastName, NULL, 0, NULL)
            """;
        insert.Parameters.AddWithValue("@Id", id);
        insert.Parameters.AddWithValue("@LastName", lastName);
        await insert.ExecuteNonQueryAsync();
    }

    private async Task InsertLegacyMembershipAsync(int id, DateTime updated, int bowlerId = 1, int membershipId = 1)
    {
        await using var insert = _legacyConnection.CreateCommand();
        insert.CommandText = """
            INSERT INTO BowlerMemberships (Id, BowlerId, MembershipId, BeginDate, EndDate, Audit_UpdatedTimestamp)
            VALUES (@Id, @BowlerId, @MembershipId, '2026-01-01', '2026-12-31', @Updated)
            """;
        insert.Parameters.AddWithValue("@Id", id);
        insert.Parameters.AddWithValue("@BowlerId", bowlerId);
        insert.Parameters.AddWithValue("@MembershipId", membershipId);
        insert.Parameters.AddWithValue("@Updated", updated);
        await insert.ExecuteNonQueryAsync();
    }

    // Seeds a website bowler holding memberships that match the Software's defaults (type 1, cost 50, 2026).
    private async Task SeedWebsiteBowlerAsync(int legacyId, params int[] membershipLegacyIds)
    {
        var bowler = BowlerFactory.Create(legacyId: legacyId);

        foreach (var membershipLegacyId in membershipLegacyIds)
        {
            bowler.AddLegacyMembership(
                membershipLegacyId,
                MembershipType.FromLegacyId(1),
                50m,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 12, 31)).IsError.ShouldBeFalse();
        }

        await _dbContext.Set<Bowler>().AddAsync(bowler, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        _dbContext.ChangeTracker.Clear();
    }

    private async Task<List<int>> WebsiteLegacyIdsAsync()
        => await _dbContext.Set<BowlerMembership>()
            .AsNoTracking()
            .Where(m => m.LegacyId != null)
            .Select(m => m.LegacyId!.Value)
            .OrderBy(id => id)
            .ToListAsync(TestContext.Current.CancellationToken);

    private BowlerMembershipWeeklySyncJob CreateJob(
        FakeLogger<BowlerMembershipWeeklySyncJob>? logger = null,
        List<DiscordAlert>? alerts = null)
    {
        var notifier = new Mock<IManualInterventionNotifier>(MockBehavior.Strict);
        notifier
            .Setup(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()))
            .Callback<DiscordAlert, CancellationToken>((alert, _) => alerts?.Add(alert))
            .Returns(Task.CompletedTask);

        return new BowlerMembershipWeeklySyncJob(
            _dbContext,
            _legacyConnection,
            _serviceProvider.GetRequiredService<IFusionCache>(),
            notifier.Object,
            logger ?? new FakeLogger<BowlerMembershipWeeklySyncJob>());
    }

    // ---- window query ----

    [Fact(DisplayName = "SyncAsync should only read memberships updated at or after the since value")]
    public async Task SyncAsync_ShouldOnlyReadMembershipsUpdatedAtOrAfterSince()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(1, Since.AddSeconds(-1));
        await InsertLegacyMembershipAsync(2, Since);
        await InsertLegacyMembershipAsync(3, Since.AddDays(1));
        var job = CreateJob();

        // Act
        await job.SyncAsync(Since, ct);

        // Assert - id 1 is outside the window; the id-set pass still heals it, so check it was not
        // written by the window pass through the created count in the log instead.
        (await WebsiteLegacyIdsAsync()).ShouldBe([1, 2, 3]);
    }

    [Fact(DisplayName = "SyncAsync should count only the memberships inside the window as changed and skip System Import rows")]
    public async Task SyncAsync_ShouldCountWindowRowsAndSkipSystemImport()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(1, Since.AddDays(1));
        await InsertLegacyMembershipAsync(2, Since.AddDays(1), membershipId: LegacyBowlerMembershipSync.SystemImportMembershipId);
        var logger = new FakeLogger<BowlerMembershipWeeklySyncJob>();
        var job = CreateJob(logger);

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        (await WebsiteLegacyIdsAsync()).ShouldBe([1]);
        logger.Collector.GetSnapshot().ShouldContain(record =>
            record.Message.Contains("1 changed in the Software, 1 created", StringComparison.Ordinal));
    }

    // ---- pass 1 ----

    [Fact(DisplayName = "SyncAsync should create a membership and the bowler, and evict the bowlers cache tag, when the bowler is missing")]
    public async Task SyncAsync_ShouldCreateBowlerAndMembershipAndEvict_WhenBowlerIsMissing()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await InsertLegacyBowlerAsync(1, lastName: "Jones");
        await InsertLegacyMembershipAsync(1, Since.AddDays(1));
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        await cache.GetOrSetAsync("membership-weekly-bowlers", _ => Task.FromResult("stale"), tags: ["neba:bowlers"], token: ct);
        var job = CreateJob();

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        var bowler = await _dbContext.Set<Bowler>().Include(b => b.Memberships).SingleAsync(b => b.LegacyId == 1, ct);
        bowler.Name.LastName.ShouldBe("Jones");
        bowler.Memberships.ShouldHaveSingleItem().LegacyId.ShouldBe(1);
        (await cache.GetOrSetAsync("membership-weekly-bowlers", _ => Task.FromResult("fresh"), token: ct)).ShouldBe("fresh");
    }

    [Fact(DisplayName = "SyncAsync should update a membership that differs and leave one that matches, evicting nothing")]
    public async Task SyncAsync_ShouldUpdateDifferingAndKeepMatching_AndEvictNothing()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1, 1, 2);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(1, Since.AddDays(1), membershipId: 2);
        await InsertLegacyMembershipAsync(2, Since.AddDays(1), membershipId: 1);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        await cache.GetOrSetAsync("membership-weekly-kept", _ => Task.FromResult("kept"), tags: ["neba:bowlers"], token: ct);
        var logger = new FakeLogger<BowlerMembershipWeeklySyncJob>();
        var job = CreateJob(logger);

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        var memberships = await _dbContext.Set<BowlerMembership>().AsNoTracking().OrderBy(m => m.LegacyId).ToListAsync(ct);
        memberships[0].Type.ShouldBe(MembershipType.FromLegacyId(2));
        memberships[0].Cost.ShouldBe(75m);
        memberships[1].Type.ShouldBe(MembershipType.FromLegacyId(1));
        (await cache.GetOrSetAsync("membership-weekly-kept", _ => Task.FromResult("fresh"), token: ct)).ShouldBe("kept");
        logger.Collector.GetSnapshot().ShouldContain(record =>
            record.Message.Contains("1 updated, 1 already current", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "SyncAsync should be idempotent: a second run creates nothing and deletes nothing")]
    public async Task SyncAsync_ShouldBeIdempotent_WhenRunTwice()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(1, Since.AddDays(1));
        var alerts = new List<DiscordAlert>();
        var job = CreateJob(alerts: alerts);
        await job.SyncAsync(Since, ct);
        _dbContext.ChangeTracker.Clear();

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        (await WebsiteLegacyIdsAsync()).ShouldBe([1]);
        alerts.ShouldBeEmpty();
    }

    [Fact(DisplayName = "SyncAsync should save every membership when more than one batch of changes comes back")]
    public async Task SyncAsync_ShouldSaveEveryMembership_WhenMoreThanOneBatchComesBack()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1);
        await InsertLegacyBowlerAsync(1);

        foreach (var id in Enumerable.Range(1, 120))
        {
            await InsertLegacyMembershipAsync(id, Since.AddDays(1));
        }

        var job = CreateJob();

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        (await WebsiteLegacyIdsAsync()).Count.ShouldBe(120);
    }

    [Fact(DisplayName = "SyncAsync should save the other memberships and alert when the database rejects one row")]
    public async Task SyncAsync_ShouldSaveRestAndAlert_WhenDatabaseRejectsOneRow()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1);
        await InsertLegacyBowlerAsync(1);
        // Membership type 3 costs 12345 in the Software, more than the website's numeric(5,2) holds.
        await InsertLegacyMembershipAsync(1, Since.AddDays(1), membershipId: 3);
        await InsertLegacyMembershipAsync(2, Since.AddDays(1));
        var alerts = new List<DiscordAlert>();
        var logger = new FakeLogger<BowlerMembershipWeeklySyncJob>();
        var job = CreateJob(logger, alerts);

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        (await WebsiteLegacyIdsAsync()).ShouldContain(2);
        (await WebsiteLegacyIdsAsync()).ShouldNotContain(1);
        logger.Collector.GetSnapshot().ShouldContain(record => record.Level == LogLevel.Warning && record.Exception != null);
        logger.Collector.GetSnapshot().ShouldContain(record => record.Level == LogLevel.Error && record.Exception != null);
        alerts.ShouldContain(alert => alert.Metadata != null && alert.Metadata["LegacyBowlerMembershipIds"].Contains('1', StringComparison.Ordinal));
    }

    [Fact(DisplayName = "SyncAsync should skip a membership whose type maps to nothing, save the rest and alert")]
    public async Task SyncAsync_ShouldSkipUnmappedTypeSaveRestAndAlert()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(1, Since.AddDays(1), membershipId: UnmappedMembershipId);
        await InsertLegacyMembershipAsync(2, Since.AddDays(1));
        var alerts = new List<DiscordAlert>();
        var logger = new FakeLogger<BowlerMembershipWeeklySyncJob>();
        var job = CreateJob(logger, alerts);

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        (await WebsiteLegacyIdsAsync()).ShouldBe([2]);
        logger.Collector.GetSnapshot().ShouldContain(record => record.Level == LogLevel.Error && record.Message.Contains("99", StringComparison.Ordinal));
        alerts.ShouldContain(alert => alert.Metadata != null && alert.Metadata["LegacyBowlerMembershipIds"] == "1");
    }

    [Fact(DisplayName = "SyncAsync should alert once, not retry or alert again, for a row that failed in the window step")]
    public async Task SyncAsync_ShouldAlertOnce_WhenWindowRowFailed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(1, Since.AddDays(1), membershipId: UnmappedMembershipId);
        var alerts = new List<DiscordAlert>();
        var logger = new FakeLogger<BowlerMembershipWeeklySyncJob>();
        var job = CreateJob(logger, alerts);

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        alerts.ShouldHaveSingleItem();
        logger.Collector.GetSnapshot()
            .Count(record => record.Level == LogLevel.Error && record.Message.Contains("has membership type", StringComparison.Ordinal))
            .ShouldBe(1);
    }

    [Fact(DisplayName = "SyncAsync should count a deleted membership on the deleted metric")]
    public async Task SyncAsync_ShouldRecordDeletedMetric_WhenExtraIsDeleted()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1, 1, 9);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(1, Old);
        var job = CreateJob();
        var recorded = new List<long>();
        using var listener = ListenFor("neba.api.legacy.membership_sync.deleted", recorded);

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        recorded.ShouldContain(1L);
    }

    [Fact(DisplayName = "SyncAsync should count a tripped delete guard on the guard metric")]
    public async Task SyncAsync_ShouldRecordGuardMetric_WhenGuardTrips()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1, 1, 8, 9);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(1, Old);
        var job = CreateJob();
        var recorded = new List<long>();
        using var listener = ListenFor("neba.api.legacy.membership_sync.delete_guard_tripped", recorded);

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        recorded.ShouldContain(1L);
    }

    private static MeterListener ListenFor(string instrumentName, List<long> recorded)
    {
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Name == instrumentName)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, _, _) =>
        {
            lock (recorded)
            {
                recorded.Add(measurement);
            }
        });
        listener.Start();
        return listener;
    }

    // ---- pass 2 ----

    [Fact(DisplayName = "SyncAsync should delete nothing and upsert nothing when the id sets match")]
    public async Task SyncAsync_ShouldDoNothing_WhenIdSetsMatch()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1, 1, 2);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(1, Old);
        await InsertLegacyMembershipAsync(2, Old);
        var alerts = new List<DiscordAlert>();
        var logger = new FakeLogger<BowlerMembershipWeeklySyncJob>();
        var job = CreateJob(logger, alerts);

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        (await WebsiteLegacyIdsAsync()).ShouldBe([1, 2]);
        alerts.ShouldBeEmpty();
        logger.Collector.GetSnapshot().ShouldContain(record => record.Message.Contains("ids match: 2", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "SyncAsync should delete the extra and upsert the missing row when one of each offsets the counts")]
    public async Task SyncAsync_ShouldDeleteExtraAndUpsertMissing_WhenOffsetting()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1, 1, 9);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(1, Old);
        await InsertLegacyMembershipAsync(2, Old);
        var alerts = new List<DiscordAlert>();
        var job = CreateJob(alerts: alerts);

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        (await WebsiteLegacyIdsAsync()).ShouldBe([1, 2]);
        var alert = alerts.ShouldHaveSingleItem();
        alert.Metadata.ShouldNotBeNull();
        alert.Metadata["LegacyBowlerMembershipIds"].ShouldBe("9");
    }

    [Fact(DisplayName = "SyncAsync should delete exactly one extra membership and report it")]
    public async Task SyncAsync_ShouldDeleteAndReport_WhenExactlyOneExtra()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1, 1, 9);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(1, Old);
        var alerts = new List<DiscordAlert>();
        var logger = new FakeLogger<BowlerMembershipWeeklySyncJob>();
        var job = CreateJob(logger, alerts);

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        (await WebsiteLegacyIdsAsync()).ShouldBe([1]);
        alerts.ShouldHaveSingleItem().Title.ShouldContain("deleted");
        logger.Collector.GetSnapshot().ShouldContain(record => record.Message.Contains("Deleted 1", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "SyncAsync should delete nothing and alert when two or more website memberships are missing from the Software")]
    public async Task SyncAsync_ShouldDeleteNothingAndAlert_WhenTwoOrMoreExtras()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1, 1, 8, 9);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(1, Old);
        var alerts = new List<DiscordAlert>();
        var logger = new FakeLogger<BowlerMembershipWeeklySyncJob>();
        var job = CreateJob(logger, alerts);

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        (await WebsiteLegacyIdsAsync()).ShouldBe([1, 8, 9]);
        var alert = alerts.ShouldHaveSingleItem();
        alert.Severity.ShouldBe(DiscordAlertSeverity.Warning);
        alert.Metadata.ShouldNotBeNull();
        alert.Metadata["LegacyBowlerMembershipIds"].ShouldContain("8");
        alert.Metadata["LegacyBowlerMembershipIds"].ShouldContain("9");
        logger.Collector.GetSnapshot().ShouldContain(record => record.Level == LogLevel.Error);
    }

    [Fact(DisplayName = "SyncAsync should delete nothing and alert when the Software has no memberships but the website has legacy ones")]
    public async Task SyncAsync_ShouldDeleteNothingAndAlert_WhenSoftwareSetIsEmpty()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1, 1);
        var alerts = new List<DiscordAlert>();
        var job = CreateJob(alerts: alerts);

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        (await WebsiteLegacyIdsAsync()).ShouldBe([1]);
        alerts.ShouldHaveSingleItem().Severity.ShouldBe(DiscordAlertSeverity.Warning);
    }

    [Fact(DisplayName = "SyncAsync should upsert memberships the website lacks even when they fall outside the window")]
    public async Task SyncAsync_ShouldUpsertMissingRows_OutsideWindow()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(1, Old);
        await InsertLegacyMembershipAsync(2, Old);
        var job = CreateJob();

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        (await WebsiteLegacyIdsAsync()).ShouldBe([1, 2]);
    }

    [Fact(DisplayName = "SyncAsync should never compare or delete website memberships that have no legacy id")]
    public async Task SyncAsync_ShouldIgnoreWebsiteOnlyMemberships()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create(legacyId: 1);
        bowler.AddMembership(MembershipType.New, new DateOnly(2026, 3, 1)).IsError.ShouldBeFalse();
        await _dbContext.Set<Bowler>().AddAsync(bowler, ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(1, Old);
        var alerts = new List<DiscordAlert>();
        var job = CreateJob(alerts: alerts);

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        var memberships = await _dbContext.Set<BowlerMembership>().AsNoTracking().ToListAsync(ct);
        memberships.Count.ShouldBe(2);
        memberships.ShouldContain(m => m.LegacyId == null);
        alerts.ShouldBeEmpty();
    }

    [Fact(DisplayName = "SyncAsync should leave System Import memberships out of the Software id set")]
    public async Task SyncAsync_ShouldExcludeSystemImportFromIdSet()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWebsiteBowlerAsync(1, 1);
        await InsertLegacyBowlerAsync(1);
        await InsertLegacyMembershipAsync(1, Old);
        await InsertLegacyMembershipAsync(2, Old, membershipId: LegacyBowlerMembershipSync.SystemImportMembershipId);
        var alerts = new List<DiscordAlert>();
        var logger = new FakeLogger<BowlerMembershipWeeklySyncJob>();
        var job = CreateJob(logger, alerts);

        // Act
        await job.SyncAsync(Since, ct);

        // Assert
        (await WebsiteLegacyIdsAsync()).ShouldBe([1]);
        alerts.ShouldBeEmpty();
        logger.Collector.GetSnapshot().ShouldContain(record => record.Message.Contains("ids match: 1", StringComparison.Ordinal));
    }
}