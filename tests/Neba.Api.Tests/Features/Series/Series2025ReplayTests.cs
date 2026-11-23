using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Database;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.CreateStageOverride;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.RecomputeSeriesAdvancement;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series;

/// <summary>
/// Replays an invented season on the 2025 Stage layout through the real chain: the Recompute handler, the Series lock, the
/// facts reader over the database, the aggregate and its stored output. Expected values are in
/// <see cref="Series2025ReplayExpected"/>, worked out by hand.
/// </summary>
[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class Series2025ReplayTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly TimeSpan Grace = TimeSpan.FromHours(24);

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
        => await fixture.ResetAsync();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static SeriesAdvancement CreateAdvancement(AppDbContext db)
        => new(
            db,
            new SeriesAdvancementFactsReader(db),
            KeyedLockDoubles.GrantingAny().Provider.Object,
            SeriesCache.Evicting().Object,
            new FakeTimeProvider(AdvancementRunHarness.Now),
            NullLogger<SeriesAdvancement>.Instance);

    private static SeriesStageId StageId(Series2025Replay replay, int order)
        => replay.Series.Stages.Single(stage => stage.Order == order).Id;

    private async Task<Series2025Replay> SeedAsync(bool withFinale)
    {
        var replay = Series2025ReferenceFactory.CreateWithResults(withFinale);

        await using var db = fixture.CreateDbContext();
        db.Seasons.Add(replay.Season);
        db.Bowlers.AddRange(replay.Bowlers.Values);
        db.Tournaments.AddRange(replay.Tournaments);
        db.SquadScores.AddRange(replay.SquadScores);
        db.Series.Add(replay.Series);
        await db.SaveChangesAsync(Ct);

        return replay;
    }

    private async Task<ErrorOr<Updated>> RecomputeAsync(Series2025Replay replay, int fromOrder, int version, bool confirm = false)
    {
        await using var db = fixture.CreateDbContext();

        var handler = new RecomputeSeriesAdvancementCommandHandler(CreateAdvancement(db));

        return await handler.HandleAsync(
            RecomputeSeriesAdvancementCommandFactory.Create(
                seriesId: replay.Series.Id,
                stageId: StageId(replay, fromOrder),
                version: version,
                confirmResetOfficial: confirm),
            Ct);
    }

    private async Task<ErrorOr<CreatedStageOverride>> OverrideAsync(
        Series2025Replay replay, OverrideKind kind, int stageOrder, int bowler, int version)
    {
        await using var db = fixture.CreateDbContext();

        var handler = new CreateStageOverrideCommandHandler(CreateAdvancement(db));

        return await handler.HandleAsync(
            CreateStageOverrideCommandFactory.Create(
                seriesId: replay.Series.Id,
                stageId: StageId(replay, stageOrder),
                kind: kind,
                bowlerId: replay.Bowlers[bowler].Id,
                version: version),
            Ct);
    }

    private async Task<ErrorOr<AdvanceResult>> MakeOfficialAsync(Series2025Replay replay)
    {
        await using var db = fixture.CreateDbContext();

        return await CreateAdvancement(db).RunWithoutFactsAsync(
            replay.Series.Id,
            (series, _, now) => series.MakeDueOfficial(now.AddDays(3), Grace),
            SeriesAdvancement.AdminLockWait,
            Ct);
    }

    private async Task<SeriesAggregate> StoredAsync(Series2025Replay replay)
    {
        await using var db = fixture.CreateDbContext();

        return await db.Series.AsNoTracking()
            .Include(series => series.Stages).ThenInclude(stage => stage.SurvivorOutcomes)
            .Include(series => series.Stages).ThenInclude(stage => stage.Overrides)
            .Include(series => series.FieldEntries)
            .Include(series => series.Positions)
            .AsSplitQuery()
            .SingleAsync(series => series.Id == replay.Series.Id, Ct);
    }

    /// <summary>Seeds, then recomputes from the first Stage. The Series ends at Version 1.</summary>
    private async Task<Series2025Replay> ReplayAsync(bool withFinale)
    {
        var replay = await SeedAsync(withFinale);

        (await RecomputeAsync(replay, 1, version: 0)).IsError.ShouldBeFalse();

        return replay;
    }

    /// <summary>Replays, then adds bowler 50 to Stage 6 and clears bowler 42 from Stage 5. The Series ends at Version 3.</summary>
    private async Task<Series2025Replay> ReplayWithOverridesAsync(bool withFinale)
    {
        var replay = await ReplayAsync(withFinale);

        (await OverrideAsync(replay, OverrideKind.Add, 6, 50, version: 1)).IsError.ShouldBeFalse();
        (await OverrideAsync(replay, OverrideKind.Clear, 5, 42, version: 2)).IsError.ShouldBeFalse();

        return replay;
    }

    private static void ShouldHaveOutput(
        SeriesAggregate stored, Series2025Replay replay, Series2025ReplayExpected expected, bool withFinale)
    {
        var number = replay.Bowlers.ToDictionary(pair => pair.Value.Id, pair => pair.Key);
        var stages = stored.Stages.OrderBy(stage => stage.Order).ToList();

        // Qualifying Stages: slot counts and field entries.
        for (var index = 0; index < expected.Qualifying.Count; index++)
        {
            var stage = stages[index];
            var stageEntries = stored.FieldEntries
                .Where(entry => entry.SourceStageId == stage.Id)
                .Select(entry => (number[entry.BowlerId], entry.Place!.Value));

            stage.SlotCount.ShouldBe(expected.Qualifying[index].SlotCount, $"Stage {stage.Order} slots");
            stageEntries.ShouldBe(expected.Qualifying[index].Entries, ignoreOrder: true, $"Stage {stage.Order} entries");
        }

        // The Backfill.
        stored.BackfillAsOf.ShouldBe(Series2025ReplayExpected.BackfillAsOf);
        stored.FieldEntries
            .Where(entry => entry.SourceStageId is null)
            .Select(entry => (number[entry.BowlerId], entry.BackfillPoints!.Value))
            .ShouldBe(expected.Backfill, ignoreOrder: true);
        stored.FieldEntries.Count.ShouldBe(expected.FieldSize);

        // The first Survivor Stage.
        var first = stages[8].SurvivorOutcomes;
        first.Count.ShouldBe(expected.FieldSize);
        first.Where(outcome => outcome.Advanced).Select(outcome => number[outcome.BowlerId])
            .ShouldBe(expected.FirstSurvivorAdvanced, ignoreOrder: true);
        first.Where(outcome => outcome.BlockTotal is null).Select(outcome => number[outcome.BowlerId])
            .ShouldBe(Series2025ReplayExpected.FirstSurvivorDidNotBowl, ignoreOrder: true);
        foreach (var (bowler, total) in Series2025ReplayExpected.FirstSurvivorTotals)
        {
            first.SingleOrDefault(outcome => outcome.BowlerId == replay.Bowlers[bowler].Id)?.BlockTotal.ShouldBe(total, $"First squad total of {bowler}");
        }

        // The second Survivor Stage runs over the first one's advancers.
        var second = stages[9].SurvivorOutcomes;
        second.Select(outcome => number[outcome.BowlerId]).ShouldBe(expected.FirstSurvivorAdvanced, ignoreOrder: true);
        second.Where(outcome => outcome.Advanced).Select(outcome => number[outcome.BowlerId])
            .ShouldBe(expected.SecondSurvivorAdvanced, ignoreOrder: true);
        second.Where(outcome => outcome.BlockTotal is null).Select(outcome => number[outcome.BowlerId])
            .ShouldBe(Series2025ReplayExpected.SecondSurvivorDidNotBowl, ignoreOrder: true);
        foreach (var (bowler, total) in Series2025ReplayExpected.SecondSurvivorTotals)
        {
            second.SingleOrDefault(outcome => outcome.BowlerId == replay.Bowlers[bowler].Id)?.BlockTotal.ShouldBe(total, $"Second squad total of {bowler}");
        }

        // The Placement Stage.
        var positions = stored.Positions.OrderBy(position => position.Position).ToList();
        positions.Select(position => position.Position).ShouldBe(Enumerable.Range(1, expected.Positions.Count));
        positions.Select(position => number[position.BowlerId]).ShouldBe(expected.Positions);
        positions.Select(position => position.Bowled)
            .ShouldBe([.. positions.Select((_, index) => index < expected.BowledPositions)]);
        positions.Select(position => position.IsFinalist)
            .ShouldBe([.. positions.Select((_, index) => withFinale ? index < 4 : (bool?)null)]);
    }

    [Theory(DisplayName = "Recompute from the first Stage should compute all eleven Stages from the invented results, with and without a Finale")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Recompute_ShouldComputeEveryStage_FromInventedResults(bool withFinale)
    {
        // Arrange
        var replay = await SeedAsync(withFinale);

        // Act
        var result = await RecomputeAsync(replay, 1, version: 0);

        // Assert
        result.IsError.ShouldBeFalse();
        var stored = await StoredAsync(replay);
        stored.Version.ShouldBe(1);
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed && stage.ComputedAt == AdvancementRunHarness.Now);
        stored.Stages.Single(stage => stage.Order == 9).SquadDesignation!.SquadId.ShouldBe(replay.Tournaments[8].Squads.Single().Id);
        stored.Stages.Single(stage => stage.Order == 10).SquadDesignation!.SquadId.ShouldBe(replay.Tournaments[9].Squads.Single().Id);
        ShouldHaveOutput(stored, replay, Series2025ReplayExpected.BeforeOverrides, withFinale);
    }

    [Theory(DisplayName = "An Add and a Clear override should rebuild the later output, and a Recompute from the first Stage should keep both and write nothing")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Recompute_ShouldKeepOverrides_AfterAddAndClear(bool withFinale)
    {
        // Arrange
        var replay = await ReplayWithOverridesAsync(withFinale);

        // Act
        var result = await RecomputeAsync(replay, 1, version: 3);

        // Assert
        result.IsError.ShouldBeFalse();
        var stored = await StoredAsync(replay);
        stored.Version.ShouldBe(3);
        ShouldHaveOutput(stored, replay, Series2025ReplayExpected.AfterOverrides, withFinale);
        var cleared = stored.Stages.Single(stage => stage.Order == 5).Overrides.ShouldHaveSingleItem();
        cleared.Kind.ShouldBe(OverrideKind.Clear);
        cleared.BowlerId.ShouldBe(replay.Bowlers[42].Id);
        cleared.Applied.ShouldBeTrue();
        var added = stored.Stages.Single(stage => stage.Order == 6).Overrides.ShouldHaveSingleItem();
        added.Kind.ShouldBe(OverrideKind.Add);
        added.BowlerId.ShouldBe(replay.Bowlers[50].Id);
        added.Applied.ShouldBeTrue();
        stored.FieldEntries.Single(entry => entry.BowlerId == replay.Bowlers[50].Id).ViaOverride.ShouldBeTrue();
    }

    [Fact(DisplayName = "MakeDueOfficial should make the ten Qualifying and Survivor Stages Official and leave the Placement Stage Computed")]
    public async Task MakeDueOfficial_ShouldMakeTenStagesOfficial_AndNeverThePlacementStage()
    {
        // Arrange
        var replay = await ReplayWithOverridesAsync(withFinale: true);

        // Act
        var result = await MakeOfficialAsync(replay);

        // Assert
        result.IsError.ShouldBeFalse();
        var stored = await StoredAsync(replay);
        stored.Version.ShouldBe(4);
        stored.Stages.Where(stage => stage.Order <= 10).ShouldAllBe(stage => stage.Status == StageStatus.Official);
        stored.Stages.Single(stage => stage.Order == 11).Status.ShouldBe(StageStatus.Computed);
    }

    [Theory(DisplayName = "Recompute should ask for confirmation and change nothing when an Official Stage is in range")]
    [InlineData(1, 10)]
    [InlineData(9, 2)]
    [InlineData(10, 1)]
    public async Task Recompute_ShouldAskForConfirmation_WhenOfficialStagesAreInRange(int fromOrder, int officialCount)
    {
        // Arrange
        var replay = await ReplayWithOverridesAsync(withFinale: true);
        (await MakeOfficialAsync(replay)).IsError.ShouldBeFalse();

        // Act
        var result = await RecomputeAsync(replay, fromOrder, version: 4);

        // Assert
        result.IsError.ShouldBeTrue();
        var error = result.FirstError;
        error.Type.ShouldBe(ErrorType.Conflict);
        error.Code.ShouldBe("Series.Recompute.ConfirmationRequired");
        ((int[])error.Metadata![SeriesAdvancementErrors.OfficialStageOrdersKey])
            .ShouldBe(Enumerable.Range(fromOrder, officialCount));
        var stored = await StoredAsync(replay);
        stored.Version.ShouldBe(4);
        stored.Stages.Where(stage => stage.Order <= 10).ShouldAllBe(stage => stage.Status == StageStatus.Official);
    }

    [Fact(DisplayName = "A confirmed Recompute from the first Survivor Stage should reset only the Survivor Stages and keep the earlier Official Stages")]
    public async Task Recompute_ShouldResetOnlyStagesInRange_WhenConfirmedFromFirstSurvivorStage()
    {
        // Arrange
        var replay = await ReplayWithOverridesAsync(withFinale: true);
        (await MakeOfficialAsync(replay)).IsError.ShouldBeFalse();

        // Act
        var result = await RecomputeAsync(replay, 9, version: 4, confirm: true);

        // Assert
        result.IsError.ShouldBeFalse();
        var stored = await StoredAsync(replay);
        stored.Version.ShouldBe(5);
        stored.Stages.Where(stage => stage.Order <= 8).ShouldAllBe(stage => stage.Status == StageStatus.Official);
        stored.Stages.Where(stage => stage.Order >= 9).ShouldAllBe(stage => stage.Status == StageStatus.Computed);
        ShouldHaveOutput(stored, replay, Series2025ReplayExpected.AfterOverrides, withFinale: true);
    }

    [Fact(DisplayName = "A confirmed Recompute from the first Stage should reset every Official Stage and rebuild the same output with the overrides")]
    public async Task Recompute_ShouldResetEveryOfficialStage_AndKeepOverrides_WhenConfirmedFromFirstStage()
    {
        // Arrange
        var replay = await ReplayWithOverridesAsync(withFinale: true);
        (await MakeOfficialAsync(replay)).IsError.ShouldBeFalse();

        // Act
        var result = await RecomputeAsync(replay, 1, version: 4, confirm: true);

        // Assert
        result.IsError.ShouldBeFalse();
        var stored = await StoredAsync(replay);
        stored.Version.ShouldBe(5);
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed);
        stored.Stages.Single(stage => stage.Order == 5).Overrides.ShouldHaveSingleItem().Applied.ShouldBeTrue();
        stored.Stages.Single(stage => stage.Order == 6).Overrides.ShouldHaveSingleItem().Applied.ShouldBeTrue();
        ShouldHaveOutput(stored, replay, Series2025ReplayExpected.AfterOverrides, withFinale: true);
    }
}