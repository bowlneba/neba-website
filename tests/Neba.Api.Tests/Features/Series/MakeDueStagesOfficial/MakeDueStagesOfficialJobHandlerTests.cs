using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.MakeDueStagesOfficial;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;

using ZiggyCreatures.Caching.Fusion;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series.MakeDueStagesOfficial;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class MakeDueStagesOfficialJobHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset ComputedAt = AdvancementRunHarness.Now;
    private static readonly TimeSpan Grace = TimeSpan.FromHours(24);

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private readonly AdvancementRunHarness _harness = new(fixture);
    private readonly List<AppDbContext> _contexts = [];

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _harness.DisposeAsync();
        await _dbContext.DisposeAsync();

        foreach (var context in _contexts)
        {
            await context.DisposeAsync();
        }
    }

    private sealed record Sweep(
        MakeDueStagesOfficialJobHandler Handler,
        AppDbContext Db,
        SaveCounter Saves,
        FakeLogger<MakeDueStagesOfficialJobHandler> Log,
        Mock<IFusionCache> Cache);

    private Sweep CreateSweep(
        DateTimeOffset now,
        int graceHours = 24,
        Mock<IKeyedLockProvider>? locks = null,
        Mock<IFusionCache>? cache = null,
        IInterceptor? extraInterceptor = null)
    {
        var saves = new SaveCounter();
        var db = extraInterceptor is null ? fixture.CreateDbContext(saves) : fixture.CreateDbContext(saves, extraInterceptor);
        _contexts.Add(db);

        var evictions = cache ?? SeriesCache.Evicting();
        var advancement = new SeriesAdvancement(
            db,
            FactsReaderDoubles.Untouched().Object,
            (locks ?? KeyedLockDoubles.GrantingAny().Provider).Object,
            evictions.Object,
            new FakeTimeProvider(now),
            NullLogger<SeriesAdvancement>.Instance);

        var log = new FakeLogger<MakeDueStagesOfficialJobHandler>();
        var handler = new MakeDueStagesOfficialJobHandler(
            db, advancement, Options.Create(new SeriesAdvancementSettings { OfficialGraceHours = graceHours }), log);

        return new Sweep(handler, db, saves, log, evictions);
    }

    private static Task ExecuteAsync(Sweep sweep)
        => sweep.Handler.ExecuteAsync(new MakeDueStagesOfficialJob(), TestContext.Current.CancellationToken);

    private static string SummaryOf(Sweep sweep)
        => sweep.Log.Collector.GetSnapshot().Single(record => record.Level == LogLevel.Information).Message;

    private static string SummaryMessage(int series, int stages, int failed)
        => $"Make Official sweep checked {series} Series, made {stages} Stage(s) Official, {failed} failed.";

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

    private async Task<(AdvancementScenario Scenario, AdvancementFactsScript Script)> SeedAsync(int year = 2025)
    {
        var scenario = await AdvancementRunSeed.SeedAsync(_dbContext, TestContext.Current.CancellationToken, year);

        return (scenario, new AdvancementFactsScript(scenario));
    }

    /// <summary>Only the Qualifying Stage is Computed (at <see cref="ComputedAt"/>). The Survivor tournament has not finished.</summary>
    private async Task<(AdvancementScenario Scenario, AdvancementFactsScript Script)> SeedQualifierComputedAsync(int year = 2025)
    {
        var (scenario, script) = await SeedAsync(year);
        script.SurvivorStatus = TournamentStatus.Scheduled;
        await AdvanceAsync(scenario, script, scenario.Qualifier, ComputedAt);

        var stored = await StoredAsync(scenario);
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Computed);
        StageOf(stored, 2).Status.ShouldBe(StageStatus.Pending);

        return (scenario, script);
    }

    /// <summary>Every Stage is Computed at <see cref="ComputedAt"/>.</summary>
    private async Task<AdvancementScenario> SeedAllComputedAsync(int year = 2025)
    {
        var (scenario, script) = await SeedAsync(year);
        await AdvanceAsync(scenario, script, scenario.Qualifier, ComputedAt);

        var stored = await StoredAsync(scenario);
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed && stage.ComputedAt == ComputedAt);

        return scenario;
    }

    private async Task AdvanceAsync(AdvancementScenario scenario, AdvancementFactsScript script, Tournament from, DateTimeOffset at)
    {
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), now: at);

        var result = await run.Run.RunAsync(
            scenario.SeriesId,
            (series, facts, now) => series.Advance(from.Id, facts, now),
            AdvancementRunHarness.Wait,
            needsFacts: true,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
    }

    private Task<SeriesAggregate> StoredAsync(AdvancementScenario scenario)
        => _harness.StoredAsync(scenario.SeriesId, TestContext.Current.CancellationToken);

    private static SeriesStage StageOf(SeriesAggregate series, int order)
        => series.Stages.Single(stage => stage.Order == order);

    [Theory(DisplayName = "ExecuteAsync should make a Stage Official when ComputedAt plus the grace period equals now")]
    [InlineData(6)]
    [InlineData(24)]
    [InlineData(168)]
    public async Task ExecuteAsync_ShouldMakeStageOfficial_WhenGraceEndsExactlyNow(int graceHours)
    {
        // Arrange
        var (scenario, _) = await SeedQualifierComputedAsync();
        var versionBefore = (await StoredAsync(scenario)).Version;
        var now = ComputedAt + TimeSpan.FromHours(graceHours);
        var sweep = CreateSweep(now, graceHours);

        // Act
        await ExecuteAsync(sweep);

        // Assert
        var stored = await StoredAsync(scenario);
        var qualifying = StageOf(stored, 1);
        qualifying.Status.ShouldBe(StageStatus.Official);
        qualifying.OfficialAt.ShouldBe(now);
        stored.Version.ShouldBe(versionBefore + 1);
        StageOf(stored, 2).Status.ShouldBe(StageStatus.Pending);
        SummaryOf(sweep).ShouldBe(SummaryMessage(series: 1, stages: 1, failed: 0));
    }

    [Theory(DisplayName = "ExecuteAsync should leave a Stage Computed when now is one tick before ComputedAt plus the grace period")]
    [InlineData(6)]
    [InlineData(24)]
    [InlineData(168)]
    public async Task ExecuteAsync_ShouldLeaveStageComputed_WhenNowIsOneTickBeforeGraceEnds(int graceHours)
    {
        // Arrange
        var (scenario, _) = await SeedQualifierComputedAsync();
        var versionBefore = (await StoredAsync(scenario)).Version;
        var now = ComputedAt + TimeSpan.FromHours(graceHours) - TimeSpan.FromTicks(1);
        var sweep = CreateSweep(now, graceHours, cache: SeriesCache.Untouched());

        // Act
        await ExecuteAsync(sweep);

        // Assert
        var stored = await StoredAsync(scenario);
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Computed);
        StageOf(stored, 1).OfficialAt.ShouldBeNull();
        stored.Version.ShouldBe(versionBefore);
        sweep.Saves.Saves.ShouldBe(0);
        SummaryOf(sweep).ShouldBe(SummaryMessage(series: 1, stages: 0, failed: 0));
    }

    [Fact(DisplayName = "ExecuteAsync should read the grace period from the settings, not use a fixed 24 hours")]
    public async Task ExecuteAsync_ShouldUseConfiguredGrace_NotTwentyFourHours()
    {
        // Arrange
        var (scenario, _) = await SeedQualifierComputedAsync();
        var sweep = CreateSweep(ComputedAt + TimeSpan.FromHours(24), graceHours: 48, cache: SeriesCache.Untouched());

        // Act
        await ExecuteAsync(sweep);

        // Assert
        StageOf(await StoredAsync(scenario), 1).Status.ShouldBe(StageStatus.Computed);
    }

    [Fact(DisplayName = "ExecuteAsync should make due Stages Official, evict the tournament cache once and log the counts")]
    public async Task ExecuteAsync_ShouldMakeQualifyingAndSurvivorOfficial_AndEvictCacheAndLogCounts()
    {
        // Arrange
        var scenario = await SeedAllComputedAsync();
        var versionBefore = (await StoredAsync(scenario)).Version;
        var now = ComputedAt + Grace;
        var sweep = CreateSweep(now);

        // Act
        await ExecuteAsync(sweep);

        // Assert
        var stored = await StoredAsync(scenario);
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Official);
        StageOf(stored, 2).Status.ShouldBe(StageStatus.Official);
        StageOf(stored, 1).OfficialAt.ShouldBe(now);
        StageOf(stored, 2).OfficialAt.ShouldBe(now);
        stored.Version.ShouldBe(versionBefore + 1);
        sweep.Saves.Saves.ShouldBe(1);
        SeriesCache.VerifyEvictedOnce(sweep.Cache);
        var record = sweep.Log.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Information);
        record.Message.ShouldBe(SummaryMessage(series: 1, stages: 2, failed: 0));
    }

    [Fact(DisplayName = "ExecuteAsync should never make a Placement Stage Official")]
    public async Task ExecuteAsync_ShouldNeverMakePlacementStageOfficial()
    {
        // Arrange
        var scenario = await SeedAllComputedAsync();
        var sweep = CreateSweep(ComputedAt + TimeSpan.FromDays(365));

        // Act
        await ExecuteAsync(sweep);

        // Assert
        var stored = await StoredAsync(scenario);
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Official);
        StageOf(stored, 2).Status.ShouldBe(StageStatus.Official);
        StageOf(stored, 3).Status.ShouldBe(StageStatus.Computed);
        StageOf(stored, 3).OfficialAt.ShouldBeNull();
    }

    [Fact(DisplayName = "ExecuteAsync should not treat a Series with only a Computed Placement Stage as a candidate")]
    public async Task ExecuteAsync_ShouldSkipSeries_WhenOnlyPlacementStageIsComputed()
    {
        // Arrange
        var scenario = await SeedAllComputedAsync();
        var firstSweep = CreateSweep(ComputedAt + Grace);
        await ExecuteAsync(firstSweep);
        var secondSweep = CreateSweep(
            ComputedAt + TimeSpan.FromDays(30), locks: KeyedLockDoubles.Untouched(), cache: SeriesCache.Untouched());

        // Act
        await ExecuteAsync(secondSweep);

        // Assert
        StageOf(await StoredAsync(scenario), 3).Status.ShouldBe(StageStatus.Computed);
        secondSweep.Saves.Saves.ShouldBe(0);
        SummaryOf(secondSweep).ShouldBe(SummaryMessage(series: 0, stages: 0, failed: 0));
    }

    [Fact(DisplayName = "ExecuteAsync should skip a Series whose Stages are all Pending")]
    public async Task ExecuteAsync_ShouldSkipSeries_WhenEveryStageIsPending()
    {
        // Arrange
        _ = await SeedAsync();
        var sweep = CreateSweep(ComputedAt + TimeSpan.FromDays(30), locks: KeyedLockDoubles.Untouched(), cache: SeriesCache.Untouched());

        // Act
        await ExecuteAsync(sweep);

        // Assert
        SummaryOf(sweep).ShouldBe(SummaryMessage(series: 0, stages: 0, failed: 0));
    }

    [Fact(DisplayName = "ExecuteAsync should not make a later Stage Official before an earlier Stage that is not yet due")]
    public async Task ExecuteAsync_ShouldNotMakeLaterStageOfficial_WhenEarlierStageIsNotDue()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        await AdvanceAsync(scenario, script, scenario.Qualifier, ComputedAt);
        var qualifierRecomputedAt = ComputedAt.AddHours(20);
        script.QualifierPlaces = [.. script.QualifierPlaces.Reverse()];
        await AdvanceAsync(scenario, script, scenario.Qualifier, qualifierRecomputedAt);
        var before = await StoredAsync(scenario);
        StageOf(before, 1).ComputedAt.ShouldBe(qualifierRecomputedAt);
        StageOf(before, 2).ComputedAt.ShouldBe(ComputedAt);
        var sweep = CreateSweep(ComputedAt + Grace + TimeSpan.FromHours(1), cache: SeriesCache.Untouched());

        // Act
        await ExecuteAsync(sweep);

        // Assert
        var stored = await StoredAsync(scenario);
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Computed);
        StageOf(stored, 2).Status.ShouldBe(StageStatus.Computed);
        sweep.Saves.Saves.ShouldBe(0);
    }

    [Fact(DisplayName = "ExecuteAsync should make both Stages Official in one sweep once the earlier Stage is due")]
    public async Task ExecuteAsync_ShouldMakeBothStagesOfficial_WhenEarlierStageBecomesDue()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        await AdvanceAsync(scenario, script, scenario.Qualifier, ComputedAt);
        var qualifierRecomputedAt = ComputedAt.AddHours(20);
        script.QualifierPlaces = [.. script.QualifierPlaces.Reverse()];
        await AdvanceAsync(scenario, script, scenario.Qualifier, qualifierRecomputedAt);
        var sweep = CreateSweep(qualifierRecomputedAt + Grace);

        // Act
        await ExecuteAsync(sweep);

        // Assert
        var stored = await StoredAsync(scenario);
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Official);
        StageOf(stored, 2).Status.ShouldBe(StageStatus.Official);
        SummaryOf(sweep).ShouldBe(SummaryMessage(series: 1, stages: 2, failed: 0));
    }

    [Fact(DisplayName = "ExecuteAsync should skip a stale Stage and still make the earlier Stage Official")]
    public async Task ExecuteAsync_ShouldSkipStaleStage_AndMakeEarlierStageOfficial()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        await AdvanceAsync(scenario, script, scenario.Qualifier, ComputedAt);
        script.SurvivorStatus = TournamentStatus.Scheduled;
        await AdvanceAsync(scenario, script, scenario.Survivor, ComputedAt.AddHours(1));
        StageOf(await StoredAsync(scenario), 2).IsStale.ShouldBeTrue();
        var sweep = CreateSweep(ComputedAt + TimeSpan.FromDays(10));

        // Act
        await ExecuteAsync(sweep);

        // Assert
        var stored = await StoredAsync(scenario);
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Official);
        StageOf(stored, 2).Status.ShouldBe(StageStatus.Computed);
        StageOf(stored, 2).IsStale.ShouldBeTrue();
        SummaryOf(sweep).ShouldBe(SummaryMessage(series: 1, stages: 1, failed: 0));
    }

    [Fact(DisplayName = "ExecuteAsync should not make a later Stage Official while an earlier Stage is stale")]
    public async Task ExecuteAsync_ShouldNotMakeLaterStageOfficial_WhenEarlierStageIsStale()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        await AdvanceAsync(scenario, script, scenario.Qualifier, ComputedAt);
        script.QualifierStatus = TournamentStatus.Scheduled;
        await AdvanceAsync(scenario, script, scenario.Qualifier, ComputedAt.AddHours(1));
        var before = await StoredAsync(scenario);
        StageOf(before, 1).IsStale.ShouldBeTrue();
        var sweep = CreateSweep(ComputedAt + TimeSpan.FromDays(10), cache: SeriesCache.Untouched());

        // Act
        await ExecuteAsync(sweep);

        // Assert
        var stored = await StoredAsync(scenario);
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Computed);
        StageOf(stored, 2).Status.ShouldBe(StageStatus.Computed);
        sweep.Saves.Saves.ShouldBe(0);
        SummaryOf(sweep).ShouldBe(SummaryMessage(series: 1, stages: 0, failed: 0));
    }

    // Bowler 8 holds slot 8 but is not a member, so the slot is vacant and the Backfill must fill it.
    private static void LeaveSlotVacant(AdvancementScenario scenario, AdvancementFactsScript script)
        => script.Members = scenario.Bowlers.Where(bowler => bowler != scenario.Bowlers[7]).ToHashSet();

    // Bowler 9 is a member outside the field with points, so the Backfill takes bowler 9.
    private static BackfillFacts ReadyBackfill(AdvancementScenario scenario)
        => BackfillFactsFactory.Create(
            asOf: scenario.Qualifier.EndDate,
            ready: true,
            points: [BowlerPointsFactFactory.Create(scenario.Bowlers[8], 50)]);

    [Fact(DisplayName = "ExecuteAsync should keep the last Qualifying Stage Computed while the Backfill has not run")]
    public async Task ExecuteAsync_ShouldKeepLastQualifyingStageComputed_WhenBackfillHasNotRun()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        LeaveSlotVacant(scenario, script);
        await AdvanceAsync(scenario, script, scenario.Qualifier, ComputedAt);
        var before = await StoredAsync(scenario);
        before.BackfillAsOf.ShouldBeNull();
        StageOf(before, 1).Status.ShouldBe(StageStatus.Computed);
        var sweep = CreateSweep(ComputedAt + TimeSpan.FromDays(10), cache: SeriesCache.Untouched());

        // Act
        await ExecuteAsync(sweep);

        // Assert
        var stored = await StoredAsync(scenario);
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Computed);
        StageOf(stored, 1).OfficialAt.ShouldBeNull();
        sweep.Saves.Saves.ShouldBe(0);
        SummaryOf(sweep).ShouldBe(SummaryMessage(series: 1, stages: 0, failed: 0));
    }

    [Fact(DisplayName = "ExecuteAsync should make the last Qualifying Stage Official once the Backfill has run and the grace period passed")]
    public async Task ExecuteAsync_ShouldMakeLastQualifyingStageOfficial_AfterBackfillRan()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        LeaveSlotVacant(scenario, script);
        await AdvanceAsync(scenario, script, scenario.Qualifier, ComputedAt);
        var waiting = CreateSweep(ComputedAt + TimeSpan.FromDays(10), cache: SeriesCache.Untouched());
        await ExecuteAsync(waiting);
        StageOf(await StoredAsync(scenario), 1).Status.ShouldBe(StageStatus.Computed);

        var backfillRanAt = ComputedAt.AddDays(11);
        script.Backfill = ReadyBackfill(scenario);
        await AdvanceAsync(scenario, script, scenario.Qualifier, backfillRanAt);
        var afterBackfill = await StoredAsync(scenario);
        afterBackfill.BackfillAsOf.ShouldBe(scenario.Qualifier.EndDate);
        StageOf(afterBackfill, 1).ComputedAt.ShouldBe(backfillRanAt);
        var sweep = CreateSweep(backfillRanAt + Grace);

        // Act
        await ExecuteAsync(sweep);

        // Assert
        var stored = await StoredAsync(scenario);
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Official);
        StageOf(stored, 2).Status.ShouldBe(StageStatus.Official);
        SummaryOf(sweep).ShouldBe(SummaryMessage(series: 1, stages: 2, failed: 0));
    }

    [Fact(DisplayName = "ExecuteAsync should change nothing and keep Version when it runs again")]
    public async Task ExecuteAsync_ShouldChangeNothing_WhenRunTwice()
    {
        // Arrange
        var (scenario, script) = await SeedQualifierComputedAsync();
        script.SurvivorStatus = TournamentStatus.Completed;
        var survivorComputedAt = ComputedAt.AddHours(10);
        await AdvanceAsync(scenario, script, scenario.Survivor, survivorComputedAt);
        var now = ComputedAt + Grace + TimeSpan.FromHours(1);
        var cache = SeriesCache.Evicting();
        await ExecuteAsync(CreateSweep(now, cache: cache));
        var afterFirst = await StoredAsync(scenario);
        StageOf(afterFirst, 1).Status.ShouldBe(StageStatus.Official);
        StageOf(afterFirst, 2).Status.ShouldBe(StageStatus.Computed);
        var second = CreateSweep(now, cache: cache);

        // Act
        await ExecuteAsync(second);

        // Assert
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBe(afterFirst.Version);
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Official);
        StageOf(stored, 1).OfficialAt.ShouldBe(StageOf(afterFirst, 1).OfficialAt);
        StageOf(stored, 2).Status.ShouldBe(StageStatus.Computed);
        second.Saves.Saves.ShouldBe(0);
        SeriesCache.VerifyEvictedOnce(cache);
        SummaryOf(second).ShouldBe(SummaryMessage(series: 1, stages: 0, failed: 0));
    }

    [Fact(DisplayName = "ExecuteAsync should log the failure with the SeriesId, still process the other Series and throw when a Series is busy")]
    public async Task ExecuteAsync_ShouldProcessOtherSeriesThenThrow_WhenOneSeriesIsBusy()
    {
        // Arrange
        var (busy, _) = await SeedQualifierComputedAsync(2025);
        var (other, _) = await SeedQualifierComputedAsync(2026);
        var locks = new Mock<IKeyedLockProvider>(MockBehavior.Strict);
        RefuseFor(locks, busy.SeriesId);
        GrantFor(locks, other.SeriesId);
        var sweep = CreateSweep(ComputedAt + Grace, locks: locks);

        // Act
        var act = () => ExecuteAsync(sweep);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldBe("Making Stages Official failed for 1 Series.");
        StageOf(await StoredAsync(other), 1).Status.ShouldBe(StageStatus.Official);
        StageOf(await StoredAsync(busy), 1).Status.ShouldBe(StageStatus.Computed);
        var records = sweep.Log.Collector.GetSnapshot();
        var warning = records.Single(record => record.Level == LogLevel.Warning);
        warning.Message.ShouldBe($"Making Stages Official failed for Series {busy.SeriesId}: {SeriesAdvancementErrors.Busy.Code}.");
        records.Single(record => record.Level == LogLevel.Information).Message.ShouldBe(SummaryMessage(series: 2, stages: 1, failed: 1));
        sweep.Db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact(DisplayName = "ExecuteAsync should log the exception with the SeriesId, still process the other Series and throw when RunAsync throws")]
    public async Task ExecuteAsync_ShouldProcessOtherSeriesThenThrow_WhenRunAsyncThrows()
    {
        // Arrange
        var (broken, _) = await SeedQualifierComputedAsync(2025);
        var (other, _) = await SeedQualifierComputedAsync(2026);
        var failure = new InvalidOperationException("The lock store is down.");
        var locks = new Mock<IKeyedLockProvider>(MockBehavior.Strict);
        ThrowFor(locks, broken.SeriesId, failure);
        GrantFor(locks, other.SeriesId);
        var sweep = CreateSweep(ComputedAt + Grace, locks: locks);

        // Act
        var act = () => ExecuteAsync(sweep);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldBe("Making Stages Official failed for 1 Series.");
        StageOf(await StoredAsync(other), 1).Status.ShouldBe(StageStatus.Official);
        StageOf(await StoredAsync(broken), 1).Status.ShouldBe(StageStatus.Computed);
        var records = sweep.Log.Collector.GetSnapshot();
        var error = records.Single(record => record.Level == LogLevel.Error);
        error.Message.ShouldBe($"Making Stages Official threw for Series {broken.SeriesId}.");
        error.Exception.ShouldBeSameAs(failure);
        records.Single(record => record.Level == LogLevel.Information).Message.ShouldBe(SummaryMessage(series: 2, stages: 1, failed: 1));
        sweep.Db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact(DisplayName = "ExecuteAsync should count a concurrent change as a failure and clear the change tracker")]
    public async Task ExecuteAsync_ShouldCountConcurrentChangeAsFailure_AndClearChangeTracker()
    {
        // Arrange
        var (scenario, _) = await SeedQualifierComputedAsync();
        var versionBefore = (await StoredAsync(scenario)).Version;
        var otherWriter = new BeforeSaveInterceptor(async ct =>
        {
            await using var job = fixture.CreateDbContext();
            await job.Series.Where(series => series.Id == scenario.SeriesId)
                .ExecuteUpdateAsync(set => set.SetProperty(series => series.Version, series => series.Version + 1), ct);
        });
        var sweep = CreateSweep(ComputedAt + Grace, cache: SeriesCache.Untouched(), extraInterceptor: otherWriter);

        // Act
        var act = () => ExecuteAsync(sweep);

        // Assert
        await Should.ThrowAsync<InvalidOperationException>(act);
        var stored = await StoredAsync(scenario);
        StageOf(stored, 1).Status.ShouldBe(StageStatus.Computed);
        stored.Version.ShouldBe(versionBefore + 1);
        sweep.Log.Collector.GetSnapshot().Single(record => record.Level == LogLevel.Warning).Message
            .ShouldBe($"Making Stages Official failed for Series {scenario.SeriesId}: {SeriesAdvancementErrors.ConcurrentChange.Code}.");
        sweep.Db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact(DisplayName = "ExecuteAsync should not fail the job when a Series is deleted during the sweep")]
    public async Task ExecuteAsync_ShouldNotFail_WhenSeriesIsDeletedDuringSweep()
    {
        // Arrange
        var (deleted, _) = await SeedQualifierComputedAsync(2025);
        var (other, _) = await SeedQualifierComputedAsync(2026);
        var locks = new Mock<IKeyedLockProvider>(MockBehavior.Strict);
        GrantFor(locks, deleted.SeriesId, before: async () =>
        {
            await using var writer = fixture.CreateDbContext();
            await writer.Series.Where(series => series.Id == deleted.SeriesId)
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        });
        GrantFor(locks, other.SeriesId);
        var sweep = CreateSweep(ComputedAt + Grace, locks: locks);

        // Act
        await ExecuteAsync(sweep);

        // Assert
        StageOf(await StoredAsync(other), 1).Status.ShouldBe(StageStatus.Official);
        var records = sweep.Log.Collector.GetSnapshot();
        records.ShouldAllBe(record => record.Level < LogLevel.Warning);
        records.Single().Message.ShouldBe(SummaryMessage(series: 2, stages: 1, failed: 0));
    }

    [Fact(DisplayName = "ExecuteAsync should let OperationCanceledException propagate without logging a summary")]
    public async Task ExecuteAsync_ShouldPropagateOperationCanceledException()
    {
        // Arrange
        var (scenario, _) = await SeedQualifierComputedAsync();
        var locks = new Mock<IKeyedLockProvider>(MockBehavior.Strict);
        ThrowFor(locks, scenario.SeriesId, new OperationCanceledException());
        var sweep = CreateSweep(ComputedAt + Grace, locks: locks, cache: SeriesCache.Untouched());

        // Act
        var act = () => ExecuteAsync(sweep);

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(act);
        sweep.Log.Collector.GetSnapshot().ShouldBeEmpty();
        sweep.Db.ChangeTracker.Entries().ShouldBeEmpty();
        StageOf(await StoredAsync(scenario), 1).Status.ShouldBe(StageStatus.Computed);
    }

    [Fact(DisplayName = "ExecuteAsync should leave the change tracker empty before each Series and after the sweep")]
    public async Task ExecuteAsync_ShouldClearChangeTrackerAfterEachSeries()
    {
        // Arrange
        var (first, _) = await SeedQualifierComputedAsync(2025);
        var (second, _) = await SeedQualifierComputedAsync(2026);
        var trackedAtLock = new List<int>();
        Sweep? sweep = null;
        var locks = new Mock<IKeyedLockProvider>(MockBehavior.Strict);
        GrantFor(locks, first.SeriesId, () => Record(trackedAtLock, () => sweep!));
        GrantFor(locks, second.SeriesId, () => Record(trackedAtLock, () => sweep!));
        sweep = CreateSweep(ComputedAt + Grace, locks: locks);

        // Act
        await ExecuteAsync(sweep);

        // Assert
        trackedAtLock.Count.ShouldBe(2);
        trackedAtLock.ShouldAllBe(count => count == 0);
        sweep.Db.ChangeTracker.Entries().ShouldBeEmpty();
        StageOf(await StoredAsync(first), 1).Status.ShouldBe(StageStatus.Official);
        StageOf(await StoredAsync(second), 1).Status.ShouldBe(StageStatus.Official);
        sweep.Cache.Verify(
            c => c.RemoveByTagAsync(Neba.Api.Caching.CacheDescriptors.Tournaments.Tag, null, It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        static Task Record(List<int> counts, Func<Sweep> sweep)
        {
            counts.Add(sweep().Db.ChangeTracker.Entries().Count());

            return Task.CompletedTask;
        }
    }

    [Fact(DisplayName = "ExecuteAsync should log a sweep of nothing when there is no Series")]
    public async Task ExecuteAsync_ShouldLogEmptySweep_WhenThereIsNoSeries()
    {
        // Arrange
        var sweep = CreateSweep(ComputedAt, locks: KeyedLockDoubles.Untouched(), cache: SeriesCache.Untouched());

        // Act
        await ExecuteAsync(sweep);

        // Assert
        SummaryOf(sweep).ShouldBe(SummaryMessage(series: 0, stages: 0, failed: 0));
    }
}