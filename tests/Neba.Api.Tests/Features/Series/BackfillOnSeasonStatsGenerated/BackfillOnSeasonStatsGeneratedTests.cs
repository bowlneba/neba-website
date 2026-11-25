using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Stats.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;

using ZiggyCreatures.Caching.Fusion;

using Handler = Neba.Api.Features.Series.BackfillOnSeasonStatsGenerated.BackfillOnSeasonStatsGenerated;
using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series.BackfillOnSeasonStatsGenerated;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class BackfillOnSeasonStatsGeneratedTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private readonly AdvancementRunHarness _harness = new(fixture);

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _harness.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    private sealed record Sweep(Handler Handler, AdvancementRun Run, FakeLogger<Handler> Log, Mock<IFusionCache> Cache)
    {
        public AppDbContext Db => Run.Db;

        public SaveCounter Saves => Run.Saves;
    }

    private Sweep CreateSweep(
        Mock<ISeriesAdvancementFactsReader> reader,
        Mock<IKeyedLockProvider>? locks = null,
        Mock<IFusionCache>? cache = null,
        IInterceptor? interceptor = null)
    {
        var evictions = cache ?? SeriesCache.Evicting();
        var run = _harness.CreateRun(reader, locks, evictions, extraInterceptor: interceptor);
        var log = new FakeLogger<Handler>();

        return new Sweep(new Handler(run.Db, run.Run, log), run, log, evictions);
    }

    private static Task ExecuteAsync(Sweep sweep, SeriesAggregate series)
        => ExecuteAsync(sweep, series.SeasonId);

    private static Task ExecuteAsync(Sweep sweep, SeasonId seasonId)
        => sweep.Handler.HandleAsync(new SeasonStatsGenerated { SeasonId = seasonId }, TestContext.Current.CancellationToken);

    private static Mock<IDisposable> NewHandle()
    {
        var handle = new Mock<IDisposable>(MockBehavior.Strict);
        handle.Setup(h => h.Dispose());

        return handle;
    }

    /// <summary>Grants the Series' job lock, after running <paramref name="before"/> if given.</summary>
    private static void GrantFor(Mock<IKeyedLockProvider> locks, SeriesId seriesId, Func<Task>? before = null)
        => locks.Setup(p => p.TryAcquireAsync(SeriesAdvancement.LockKey(seriesId), SeriesAdvancement.JobLockWait, It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                if (before is not null)
                {
                    await before();
                }

                return (IDisposable?)NewHandle().Object;
            });

    private static void RefuseFor(Mock<IKeyedLockProvider> locks, SeriesId seriesId)
        => locks.Setup(p => p.TryAcquireAsync(SeriesAdvancement.LockKey(seriesId), SeriesAdvancement.JobLockWait, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDisposable?)null);

    private static void ThrowFor(Mock<IKeyedLockProvider> locks, SeriesId seriesId, Exception exception)
        => locks.Setup(p => p.TryAcquireAsync(SeriesAdvancement.LockKey(seriesId), SeriesAdvancement.JobLockWait, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

    private static void VerifyLockedOnce(Mock<IKeyedLockProvider> locks, SeriesId seriesId)
        => locks.Verify(
            p => p.TryAcquireAsync(SeriesAdvancement.LockKey(seriesId), SeriesAdvancement.JobLockWait, It.IsAny<CancellationToken>()),
            Times.Once);

    // Bowler 8 holds slot 8 but is not a member, so the slot is vacant and the Backfill must fill it.
    private static void LeaveSlotVacant(AdvancementScenario scenario, AdvancementFactsScript script)
        => script.Members = scenario.Bowlers.Where(bowler => bowler != scenario.Bowlers[7]).ToHashSet();

    // Bowler 9 is a member outside the field with points, so the Backfill takes bowler 9.
    private static BackfillFacts ReadyBackfill(AdvancementScenario scenario, params (int BowlerIndex, int Points)[] points)
        => BackfillFactsFactory.Create(
            asOf: scenario.Qualifier.EndDate,
            ready: true,
            points: [.. (points.Length == 0 ? [(8, 50)] : points)
                .Select(p => BowlerPointsFactFactory.Create(scenario.Bowlers[p.BowlerIndex], p.Points))]);

    private async Task<(AdvancementScenario Scenario, AdvancementFactsScript Script)> SeedAsync(int year = 2025)
    {
        var scenario = await AdvancementRunSeed.SeedAsync(_dbContext, TestContext.Current.CancellationToken, year);

        return (scenario, new AdvancementFactsScript(scenario));
    }

    private async Task AdvanceAsync(AdvancementScenario scenario, AdvancementFactsScript script)
    {
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build));

        var result = await run.Run.RunAsync(
            scenario.SeriesId,
            (series, facts, now) => series.Advance(scenario.Qualifier.Id, facts, now),
            AdvancementRunHarness.Wait,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
    }

    /// <summary>The Qualifying Stage is Computed with a vacant slot and no Backfill. The later Stages wait for the Backfill.</summary>
    private async Task<(AdvancementScenario Scenario, AdvancementFactsScript Script)> SeedWaitingForBackfillAsync(int year = 2025)
    {
        var (scenario, script) = await SeedAsync(year);

        return await ComputeWithVacancyAsync(scenario, script);
    }

    private async Task<(AdvancementScenario Scenario, AdvancementFactsScript Script)> ComputeWithVacancyAsync(
        AdvancementScenario scenario, AdvancementFactsScript script)
    {
        LeaveSlotVacant(scenario, script);
        await AdvanceAsync(scenario, script);

        var stored = await StoredAsync(scenario);
        stored.BackfillAsOf.ShouldBeNull();
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Computed);
        StageOf(stored, 2).Status.ShouldBe(StageStatus.Pending);
        stored.FieldEntries.ShouldAllBe(entry => !entry.IsBackfill);

        return (scenario, script);
    }

    /// <summary>Two Series in one season, each waiting for the Backfill, and the facts each one reads.</summary>
    private async Task<(AdvancementScenario First, AdvancementScenario Second, Dictionary<SeriesId, AdvancementFactsScript> Scripts)>
        SeedPairWaitingForBackfillAsync()
    {
        var (first, firstScript) = await SeedWaitingForBackfillAsync();
        var second = await AdvancementRunSeed.SeedAnotherSeriesAsync(_dbContext, first, "Second Series", TestContext.Current.CancellationToken);
        var (_, secondScript) = await ComputeWithVacancyAsync(second, new AdvancementFactsScript(second));

        firstScript.Backfill = ReadyBackfill(first);
        secondScript.Backfill = ReadyBackfill(second);

        return (first, second, new Dictionary<SeriesId, AdvancementFactsScript>
        {
            [first.SeriesId] = firstScript,
            [second.SeriesId] = secondScript,
        });
    }

    /// <summary>The Backfill ran and the Qualifying and Survivor Stages are Official.</summary>
    private async Task<(AdvancementScenario Scenario, AdvancementFactsScript Script)> SeedOfficialAsync()
    {
        var (scenario, script) = await SeedAsync();
        LeaveSlotVacant(scenario, script);
        script.Backfill = ReadyBackfill(scenario);
        await AdvanceAsync(scenario, script);

        var official = _harness.CreateRun(FactsReaderDoubles.Untouched());
        var result = await official.Run.RunWithoutFactsAsync(
            scenario.SeriesId,
            (series, _, now) => series.MakeReadyStagesOfficial(now.AddDays(3)),
            AdvancementRunHarness.Wait,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        var stored = await StoredAsync(scenario);
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Official);
        StageOf(stored, 1).IsStale.ShouldBeFalse();
        stored.FieldEntries.ShouldContain(entry => entry.IsBackfill && entry.BowlerId == scenario.Bowlers[8]);

        return (scenario, script);
    }

    private Task<SeriesAggregate> StoredAsync(AdvancementScenario scenario)
        => _harness.StoredAsync(scenario.SeriesId, TestContext.Current.CancellationToken);

    private static SeriesStage StageOf(SeriesAggregate series, int order)
        => series.Stages.Single(stage => stage.Order == order);

    private static List<(BowlerId Bowler, SeriesStageId? Source, int? Place, int? Points, bool ViaOverride)> EntriesOf(SeriesAggregate series)
        => [.. series.FieldEntries
            .Select(entry => (entry.BowlerId, entry.SourceStageId, entry.Place, entry.BackfillPoints, entry.ViaOverride))
            .OrderBy(entry => entry.BowlerId.Value)];

    [Fact(DisplayName = "HandleAsync should run each Series of the season once under its job lock with facts, and clear the change tracker between them")]
    public async Task HandleAsync_ShouldRunEachSeriesOnce_WithFactsAndJobLockWait()
    {
        // Arrange
        var (first, second, scripts) = await SeedPairWaitingForBackfillAsync();
        var reader = FactsReaderDoubles.ReadingFor(series => scripts[series.Id].Build());
        var trackedAtLock = new List<int>();
        Sweep? sweep = null;
        var locks = new Mock<IKeyedLockProvider>(MockBehavior.Strict);
        GrantFor(locks, first.SeriesId, () => Record(trackedAtLock, () => sweep!));
        GrantFor(locks, second.SeriesId, () => Record(trackedAtLock, () => sweep!));
        sweep = CreateSweep(reader, locks);

        // Act
        await ExecuteAsync(sweep, first.Series);

        // Assert
        VerifyLockedOnce(locks, first.SeriesId);
        VerifyLockedOnce(locks, second.SeriesId);
        FactsReaderDoubles.VerifyReadTimes(reader, () => Times.Exactly(2));
        trackedAtLock.Count.ShouldBe(2);
        trackedAtLock.ShouldAllBe(count => count == 0);
        sweep.Db.ChangeTracker.Entries().ShouldBeEmpty();
        sweep.Saves.Saves.ShouldBe(2);
        (await StoredAsync(first)).BackfillAsOf.ShouldBe(first.Qualifier.EndDate);
        (await StoredAsync(second)).BackfillAsOf.ShouldBe(second.Qualifier.EndDate);
        sweep.Log.Collector.GetSnapshot().ShouldBeEmpty();

        static Task Record(List<int> counts, Func<Sweep> sweep)
        {
            counts.Add(sweep().Db.ChangeTracker.Entries().Count());

            return Task.CompletedTask;
        }
    }

    [Fact(DisplayName = "HandleAsync should leave a Series of another season untouched")]
    public async Task HandleAsync_ShouldLeaveOtherSeasonUntouched()
    {
        // Arrange
        var (target, targetScript) = await SeedWaitingForBackfillAsync(2025);
        var (other, otherScript) = await SeedWaitingForBackfillAsync(2026);
        targetScript.Backfill = ReadyBackfill(target);
        otherScript.Backfill = ReadyBackfill(other);
        var otherBefore = await StoredAsync(other);
        var reader = FactsReaderDoubles.ReadingFor(series => series.Id == target.SeriesId
            ? targetScript.Build()
            : throw new InvalidOperationException("The other season's Series must not be read."));
        var locks = new Mock<IKeyedLockProvider>(MockBehavior.Strict);
        GrantFor(locks, target.SeriesId);
        var sweep = CreateSweep(reader, locks);

        // Act
        await ExecuteAsync(sweep, target.Series);

        // Assert
        (await StoredAsync(target)).BackfillAsOf.ShouldBe(target.Qualifier.EndDate);
        var otherAfter = await StoredAsync(other);
        otherAfter.BackfillAsOf.ShouldBeNull();
        otherAfter.Version.ShouldBe(otherBefore.Version);
        EntriesOf(otherAfter).ShouldBe(EntriesOf(otherBefore));
        StageOf(otherAfter, 2).Status.ShouldBe(StageStatus.Pending);
    }

    [Fact(DisplayName = "HandleAsync should do nothing and take no lock for a season with no Series")]
    public async Task HandleAsync_ShouldDoNothing_WhenSeasonHasNoSeries()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        _ = await SeedWaitingForBackfillAsync(2025);
        var emptySeason = await SeriesSeed.AddSeasonAsync(_dbContext, 2030, ct);
        var sweep = CreateSweep(FactsReaderDoubles.Untouched(), KeyedLockDoubles.Untouched(), SeriesCache.Untouched());

        // Act
        await ExecuteAsync(sweep, emptySeason.Id);

        // Assert
        sweep.Saves.Saves.ShouldBe(0);
        sweep.Log.Collector.GetSnapshot().ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should write nothing for a Series with no Qualifying Stage")]
    public async Task HandleAsync_ShouldWriteNothing_WhenSeriesHasNoQualifyingStage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var tournament = await SeriesSeed.AddTournamentAsync(_dbContext, season, "Placement Only", new DateOnly(2025, 4, 5), null, ct);
        var series = SeriesFactory.CreateFor(season, [(tournament, StageKind.Placement, null, null, null)]);
        _dbContext.Series.Add(series);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();
        var (locks, handle) = KeyedLockDoubles.Granting(series.Id, SeriesAdvancement.JobLockWait);
        var sweep = CreateSweep(FactsReaderDoubles.Reading(() => AdvancementFactsFactory.Create()), locks, SeriesCache.Untouched());

        // Act
        await ExecuteAsync(sweep, series);

        // Assert
        sweep.Saves.Saves.ShouldBe(0);
        handle.Verify(h => h.Dispose(), Times.Once);
        var stored = await _harness.StoredAsync(series.Id, ct);
        stored.Version.ShouldBe(0);
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Pending);
        stored.FieldEntries.ShouldBeEmpty();
        sweep.Log.Collector.GetSnapshot().ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should add Backfill entries from BackfillAsOf when the last Qualifying Stage has a vacancy and the reader is ready")]
    public async Task HandleAsync_ShouldAddBackfillEntries_WhenLastQualifyingStageHasVacancyAndReaderIsReady()
    {
        // Arrange
        var (scenario, script) = await SeedWaitingForBackfillAsync();
        script.Backfill = ReadyBackfill(scenario);
        var versionBefore = (await StoredAsync(scenario)).Version;
        var cache = SeriesCache.Evicting();
        var (locks, handle) = KeyedLockDoubles.Granting(scenario.SeriesId, SeriesAdvancement.JobLockWait);
        var sweep = CreateSweep(FactsReaderDoubles.Reading(script.Build), locks, cache);

        // Act
        await ExecuteAsync(sweep, scenario.Series);

        // Assert
        var stored = await StoredAsync(scenario);
        stored.BackfillAsOf.ShouldBe(scenario.Qualifier.EndDate);
        stored.Version.ShouldBeGreaterThan(versionBefore);
        var backfill = stored.FieldEntries.Single(entry => entry.IsBackfill);
        backfill.BowlerId.ShouldBe(scenario.Bowlers[8]);
        backfill.BackfillPoints.ShouldBe(50);
        backfill.SourceStageId.ShouldBeNull();
        backfill.Place.ShouldBeNull();
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Computed);
        sweep.Saves.Saves.ShouldBe(1);
        handle.Verify(h => h.Dispose(), Times.Once);
        SeriesCache.VerifyEvictedOnce(cache);
        sweep.Log.Collector.GetSnapshot().ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should leave the Stage and Version unchanged when the reader is not ready")]
    public async Task HandleAsync_ShouldLeaveStageAndVersionUnchanged_WhenReaderIsNotReady()
    {
        // Arrange
        var (scenario, script) = await SeedWaitingForBackfillAsync();
        script.Backfill = BackfillFactsFactory.Create(
            asOf: scenario.Qualifier.EndDate,
            ready: false,
            points: [BowlerPointsFactFactory.Create(scenario.Bowlers[8], 50)]);
        var before = await StoredAsync(scenario);
        var sweep = CreateSweep(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        await ExecuteAsync(sweep, scenario.Series);

        // Assert
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBe(before.Version);
        stored.BackfillAsOf.ShouldBeNull();
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Computed);
        StageOf(stored, 1).IsStale.ShouldBeFalse();
        StageOf(stored, 2).Status.ShouldBe(StageStatus.Pending);
        EntriesOf(stored).ShouldBe(EntriesOf(before));
        sweep.Saves.Saves.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should leave an Official last Qualifying Stage unstale when the dry compute gives the same result")]
    public async Task HandleAsync_ShouldLeaveOfficialStageUnstale_WhenResultIsIdentical()
    {
        // Arrange
        var (scenario, script) = await SeedOfficialAsync();
        var before = await StoredAsync(scenario);
        var sweep = CreateSweep(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        await ExecuteAsync(sweep, scenario.Series);

        // Assert
        var stored = await StoredAsync(scenario);
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Official);
        StageOf(stored, 1).IsStale.ShouldBeFalse();
        stored.Version.ShouldBe(before.Version);
        EntriesOf(stored).ShouldBe(EntriesOf(before));
        sweep.Saves.Saves.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should mark an Official last Qualifying Stage Stale and keep its output when the dry compute differs")]
    public async Task HandleAsync_ShouldMarkOfficialStageStale_WhenResultDiffers()
    {
        // Arrange
        var (scenario, script) = await SeedOfficialAsync();
        var before = await StoredAsync(scenario);
        script.Backfill = ReadyBackfill(scenario, (8, 50), (9, 90));
        var sweep = CreateSweep(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        await ExecuteAsync(sweep, scenario.Series);

        // Assert
        var stored = await StoredAsync(scenario);
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Official);
        StageOf(stored, 1).IsStale.ShouldBeTrue();
        StageOf(stored, 2).IsStale.ShouldBeFalse();
        stored.Version.ShouldBeGreaterThan(before.Version);
        EntriesOf(stored).ShouldBe(EntriesOf(before));
        stored.FieldEntries.Single(entry => entry.IsBackfill).BowlerId.ShouldBe(scenario.Bowlers[8]);
        sweep.Saves.Saves.ShouldBe(1);
    }

    [Fact(DisplayName = "HandleAsync should give the same output and write nothing when the event is delivered again to a Computed Stage")]
    public async Task HandleAsync_ShouldGiveSameOutput_WhenEventIsRedelivered()
    {
        // Arrange
        var (scenario, script) = await SeedWaitingForBackfillAsync();
        script.Backfill = ReadyBackfill(scenario);
        await ExecuteAsync(CreateSweep(FactsReaderDoubles.Reading(script.Build)), scenario.Series);
        var afterFirst = await StoredAsync(scenario);
        var redelivery = CreateSweep(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        await ExecuteAsync(redelivery, scenario.Series);

        // Assert
        var stored = await StoredAsync(scenario);
        afterFirst.FieldEntries.ShouldContain(entry => entry.IsBackfill);
        stored.Version.ShouldBe(afterFirst.Version);
        stored.BackfillAsOf.ShouldBe(afterFirst.BackfillAsOf);
        EntriesOf(stored).ShouldBe(EntriesOf(afterFirst));
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed && !stage.IsStale);
        redelivery.Saves.Saves.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should log the SeriesId, still run the other Series and throw when a Series is busy")]
    public async Task HandleAsync_ShouldRunOtherSeriesThenThrow_WhenOneSeriesIsBusy()
    {
        // Arrange
        var (busy, other, scripts) = await SeedPairWaitingForBackfillAsync();
        var reader = FactsReaderDoubles.ReadingFor(series => scripts[series.Id].Build());
        var locks = new Mock<IKeyedLockProvider>(MockBehavior.Strict);
        RefuseFor(locks, busy.SeriesId);
        GrantFor(locks, other.SeriesId);
        var sweep = CreateSweep(reader, locks);

        // Act
        Task Act() => ExecuteAsync(sweep, busy.Series);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(Act);
        exception.Message.ShouldBe("Backfill failed for 1 Series.");
        (await StoredAsync(other)).BackfillAsOf.ShouldBe(other.Qualifier.EndDate);
        (await StoredAsync(busy)).BackfillAsOf.ShouldBeNull();
        FactsReaderDoubles.VerifyReadTimes(reader, Times.Once);
        var record = sweep.Log.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Error);
        record.Message.ShouldBe($"Backfill failed for Series {busy.SeriesId}.");
        record.Exception.ShouldNotBeNull().Message.ShouldContain(SeriesAdvancementErrors.Busy.Code);
        sweep.Db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should count a concurrent change as a failure, still run the other Series and throw")]
    public async Task HandleAsync_ShouldCountConcurrentChange_AndRunOtherSeries()
    {
        // Arrange
        var (first, second, scripts) = await SeedPairWaitingForBackfillAsync();
        var otherWriter = new BeforeSaveInterceptor(async ct =>
        {
            await using var writer = fixture.CreateDbContext();
            await writer.Series.ExecuteUpdateAsync(set => set.SetProperty(series => series.Version, series => series.Version + 1), ct);
        });
        var sweep = CreateSweep(
            FactsReaderDoubles.ReadingFor(series => scripts[series.Id].Build()),
            interceptor: otherWriter);

        // Act
        Task Act() => ExecuteAsync(sweep, first.Series);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(Act);
        exception.Message.ShouldBe("Backfill failed for 1 Series.");
        var storedFirst = await StoredAsync(first);
        var storedSecond = await StoredAsync(second);
        var failed = storedFirst.BackfillAsOf is null ? first : second;
        new[] { storedFirst.BackfillAsOf, storedSecond.BackfillAsOf }.Count(asOf => asOf is null).ShouldBe(1);
        var record = sweep.Log.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Error);
        record.Message.ShouldBe($"Backfill failed for Series {failed.SeriesId}.");
        record.Exception.ShouldNotBeNull().Message.ShouldContain(SeriesAdvancementErrors.ConcurrentChange.Code);
        sweep.Db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should log the exception, still run the other Series and throw when the facts reader throws InvalidOperationException")]
    public async Task HandleAsync_ShouldRunOtherSeriesThenThrow_WhenReaderThrows()
    {
        // Arrange
        var (broken, other, scripts) = await SeedPairWaitingForBackfillAsync();
        var failure = new InvalidOperationException("The facts store is down.");
        var reader = FactsReaderDoubles.ReadingFor(series => series.Id == broken.SeriesId
            ? throw failure
            : scripts[series.Id].Build());
        var (locks, handle) = KeyedLockDoubles.GrantingAny();
        var sweep = CreateSweep(reader, locks);

        // Act
        Task Act() => ExecuteAsync(sweep, broken.Series);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(Act);
        exception.Message.ShouldBe("Backfill failed for 1 Series.");
        (await StoredAsync(other)).BackfillAsOf.ShouldBe(other.Qualifier.EndDate);
        (await StoredAsync(broken)).BackfillAsOf.ShouldBeNull();
        handle.Verify(h => h.Dispose(), Times.Exactly(2));
        var record = sweep.Log.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Error);
        record.Message.ShouldBe($"Backfill failed for Series {broken.SeriesId}.");
        record.Exception.ShouldBeSameAs(failure);
        sweep.Db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should count every Series that failed in the final message")]
    public async Task HandleAsync_ShouldCountEveryFailure()
    {
        // Arrange
        var (first, second, _) = await SeedPairWaitingForBackfillAsync();
        var reader = FactsReaderDoubles.ReadingFor(_ => throw new InvalidOperationException("The facts store is down."));
        var sweep = CreateSweep(reader);

        // Act
        Task Act() => ExecuteAsync(sweep, first.Series);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(Act);
        exception.Message.ShouldBe("Backfill failed for 2 Series.");
        sweep.Log.Collector.GetSnapshot().Count(record => record.Level == LogLevel.Error).ShouldBe(2);
        (await StoredAsync(first)).BackfillAsOf.ShouldBeNull();
        (await StoredAsync(second)).BackfillAsOf.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should not count a Series deleted between the id query and the run as a failure")]
    public async Task HandleAsync_ShouldNotFail_WhenSeriesIsDeletedBeforeItsRun()
    {
        // Arrange
        var (deleted, other, scripts) = await SeedPairWaitingForBackfillAsync();
        var reader = FactsReaderDoubles.ReadingFor(series => scripts[series.Id].Build());
        var locks = new Mock<IKeyedLockProvider>(MockBehavior.Strict);
        GrantFor(locks, deleted.SeriesId, before: async () =>
        {
            await using var writer = fixture.CreateDbContext();
            await writer.Series.Where(series => series.Id == deleted.SeriesId)
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        });
        GrantFor(locks, other.SeriesId);
        var sweep = CreateSweep(reader, locks);

        // Act
        await ExecuteAsync(sweep, deleted.Series);

        // Assert
        (await StoredAsync(other)).BackfillAsOf.ShouldBe(other.Qualifier.EndDate);
        FactsReaderDoubles.VerifyReadTimes(reader, Times.Once);
        sweep.Log.Collector.GetSnapshot().ShouldBeEmpty();
        sweep.Db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should let OperationCanceledException propagate without logging or counting it")]
    public async Task HandleAsync_ShouldPropagateOperationCanceledException()
    {
        // Arrange
        var (scenario, script) = await SeedWaitingForBackfillAsync();
        script.Backfill = ReadyBackfill(scenario);
        var locks = new Mock<IKeyedLockProvider>(MockBehavior.Strict);
        ThrowFor(locks, scenario.SeriesId, new OperationCanceledException());
        var sweep = CreateSweep(FactsReaderDoubles.Untouched(), locks, SeriesCache.Untouched());

        // Act
        Task Act() => ExecuteAsync(sweep, scenario.Series);

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(Act);
        sweep.Log.Collector.GetSnapshot().ShouldBeEmpty();
        sweep.Db.ChangeTracker.Entries().ShouldBeEmpty();
        (await StoredAsync(scenario)).BackfillAsOf.ShouldBeNull();
    }

#nullable disable
    [Fact(DisplayName = "HandleAsync should throw ArgumentNullException when the event is null")]
    public async Task HandleAsync_ShouldThrowArgumentNullException_WhenEventIsNull()
    {
        // Arrange
        var sweep = CreateSweep(FactsReaderDoubles.Untouched(), KeyedLockDoubles.Untouched(), SeriesCache.Untouched());

        // Act
        Task Act() => sweep.Handler.HandleAsync(null, TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(Act);
    }
#nullable enable
}