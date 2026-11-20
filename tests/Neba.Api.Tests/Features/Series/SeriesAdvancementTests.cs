using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class SeriesAdvancementTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly TimeSpan Grace = TimeSpan.FromHours(24);

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

    private static AdvancementOperation AdvanceFrom(Tournament tournament)
        => (series, facts, now) => series.Advance(tournament.Id, facts, now);

    private static AdvancementOperation MakeOfficial()
        => (series, _, now) => series.MakeDueOfficial(now.AddDays(3), Grace);

    private static Task<ErrorOr<AdvanceResult>> RunAsync(
        AdvancementRun run, AdvancementScenario scenario, AdvancementOperation operation, bool needsFacts = true)
        => run.Run.RunAsync(scenario.SeriesId, operation, AdvancementRunHarness.Wait, needsFacts, TestContext.Current.CancellationToken);

    private async Task<(AdvancementScenario Scenario, AdvancementFactsScript Script)> SeedAsync()
    {
        var scenario = await AdvancementRunSeed.SeedAsync(_dbContext, TestContext.Current.CancellationToken);

        return (scenario, new AdvancementFactsScript(scenario));
    }

    private async Task<ErrorOr<AdvanceResult>> AdvanceQualifierAsync(AdvancementScenario scenario, AdvancementFactsScript script)
    {
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build));

        return await RunAsync(run, scenario, AdvanceFrom(scenario.Qualifier));
    }

    private async Task<SeriesAggregate> StoredAsync(AdvancementScenario scenario)
        => await _harness.StoredAsync(scenario.SeriesId, TestContext.Current.CancellationToken);

    [Fact(DisplayName = "RunAsync should leave a Stage Pending and save nothing when its predecessor is not Computed or Official")]
    public async Task RunAsync_ShouldLeaveStagePending_WhenPredecessorIsNotComputed()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        script.SurvivorStatus = TournamentStatus.Scheduled;
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        var result = await RunAsync(run, scenario, AdvanceFrom(scenario.Placement));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeFalse();
        result.Value.ComputedStageIds.ShouldBeEmpty();
        run.Saves.Saves.ShouldBe(0);
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBe(0);
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Pending);
        stored.FieldEntries.ShouldBeEmpty();
        stored.Positions.ShouldBeEmpty();
    }

    [Fact(DisplayName = "RunAsync should keep a Survivor Stage Pending while its predecessor is Pending, but save the squad it resolved")]
    public async Task RunAsync_ShouldKeepSurvivorStagePending_AndSaveResolvedSquad()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        script.QualifierStatus = TournamentStatus.Scheduled;
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        var result = await RunAsync(run, scenario, AdvanceFrom(scenario.Survivor));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeTrue();
        result.Value.StatusChanged.ShouldBeFalse();
        result.Value.ComputedStageIds.ShouldBeEmpty();
        run.Saves.Saves.ShouldBe(1);
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBe(result.Value.Version);
        stored.Version.ShouldBeGreaterThan(0);
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Pending);
        stored.Stages.Single(stage => stage.Order == 2).SquadDesignation!.SquadId.ShouldBe(scenario.SquadId);
        stored.FieldEntries.ShouldBeEmpty();
    }

    [Fact(DisplayName = "RunAsync should write nothing and keep Version when the same facts are delivered again")]
    public async Task RunAsync_ShouldWriteNothing_WhenFactsAreRedelivered()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        (await AdvanceQualifierAsync(scenario, script)).IsError.ShouldBeFalse();
        var versionAfterFirstRun = (await StoredAsync(scenario)).Version;
        var redelivery = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        var result = await RunAsync(redelivery, scenario, AdvanceFrom(scenario.Qualifier));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeFalse();
        redelivery.Saves.Saves.ShouldBe(0);
        versionAfterFirstRun.ShouldBeGreaterThan(0);
        (await StoredAsync(scenario)).Version.ShouldBe(versionAfterFirstRun);
    }

    [Fact(DisplayName = "RunAsync should compute every Stage in one save and evict the tournament cache when an earlier tournament advances")]
    public async Task RunAsync_ShouldComputeEveryLaterStageInOneSave_WhenQualifierAdvances()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        var cache = SeriesCache.Evicting();
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: cache);

        // Act
        var result = await RunAsync(run, scenario, AdvanceFrom(scenario.Qualifier));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeTrue();
        result.Value.StatusChanged.ShouldBeTrue();
        result.Value.ComputedStageIds.ShouldBe([scenario.Stage(1).Id, scenario.Stage(2).Id, scenario.Stage(3).Id]);
        run.Saves.Saves.ShouldBe(1);
        SeriesCache.VerifyEvictedOnce(cache);
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBe(result.Value.Version);
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed);
        stored.FieldEntries.Count.ShouldBe(8);
        stored.Stages.Single(stage => stage.Order == 2).SurvivorOutcomes.Count.ShouldBe(8);
        stored.Positions.Count.ShouldBe(8);
    }

    [Fact(DisplayName = "RunAsync should compute a Pending Stage when the earlier Stage was already Computed")]
    public async Task RunAsync_ShouldComputePendingStage_WhenEarlierStageWasComputed()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        script.SurvivorStatus = TournamentStatus.Scheduled;
        script.PlacementStatus = TournamentStatus.Scheduled;
        (await AdvanceQualifierAsync(scenario, script)).Value.ComputedStageIds.ShouldBe([scenario.Stage(1).Id]);
        script.SurvivorStatus = TournamentStatus.Completed;
        script.PlacementStatus = TournamentStatus.Completed;
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build));

        // Act
        var result = await RunAsync(run, scenario, AdvanceFrom(scenario.Qualifier));

        // Assert
        result.Value.ComputedStageIds.ShouldBe([scenario.Stage(2).Id, scenario.Stage(3).Id]);
        result.Value.StatusChanged.ShouldBeTrue();
        run.Saves.Saves.ShouldBe(1);
        (await StoredAsync(scenario)).Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed);
    }

    [Fact(DisplayName = "RunAsync should not evict the tournament cache when a recompute changes output but no status")]
    public async Task RunAsync_ShouldNotEvictCache_WhenOnlyOutputChanges()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        (await AdvanceQualifierAsync(scenario, script)).IsError.ShouldBeFalse();
        script.QualifierPlaces = [script.QualifierPlaces[1], script.QualifierPlaces[0], .. script.QualifierPlaces.Skip(2)];
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        var result = await RunAsync(run, scenario, AdvanceFrom(scenario.Qualifier));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeTrue();
        result.Value.StatusChanged.ShouldBeFalse();
        result.Value.ComputedStageIds.ShouldBe([scenario.Stage(1).Id]);
        var places = (await StoredAsync(scenario)).FieldEntries.ToDictionary(entry => entry.BowlerId, entry => entry.Place);
        places[script.QualifierPlaces[0]].ShouldBe(1);
        places[script.QualifierPlaces[1]].ShouldBe(2);
    }

    [Fact(DisplayName = "RunAsync should recompute the later Computed Stages in the same save when an earlier Stage's output changes")]
    public async Task RunAsync_ShouldRecomputeLaterComputedStages_WhenEarlierOutputChanges()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        (await AdvanceQualifierAsync(scenario, script)).IsError.ShouldBeFalse();
        var leaving = script.QualifierPlaces[7];
        var joining = scenario.Bowlers[8];
        script.QualifierPlaces = [.. script.QualifierPlaces.Take(7), joining];
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        var result = await RunAsync(run, scenario, AdvanceFrom(scenario.Qualifier));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ComputedStageIds.ShouldBe([scenario.Stage(1).Id, scenario.Stage(2).Id, scenario.Stage(3).Id]);
        result.Value.StatusChanged.ShouldBeFalse();
        run.Saves.Saves.ShouldBe(1);
        var stored = await StoredAsync(scenario);
        stored.FieldEntries.Select(entry => entry.BowlerId).ShouldContain(joining);
        stored.FieldEntries.Select(entry => entry.BowlerId).ShouldNotContain(leaving);
        stored.Stages.Single(stage => stage.Order == 2).SurvivorOutcomes.Select(outcome => outcome.BowlerId).ShouldContain(joining);
        stored.Stages.Single(stage => stage.Order == 2).SurvivorOutcomes.Select(outcome => outcome.BowlerId).ShouldNotContain(leaving);
        stored.Positions.Select(position => position.BowlerId).ShouldContain(joining);
        stored.Positions.Select(position => position.BowlerId).ShouldNotContain(leaving);
    }

    [Fact(DisplayName = "RunAsync should mark an Official Stage Stale and leave its output alone when a dry compute differs")]
    public async Task RunAsync_ShouldMarkOfficialStageStale_WhenDryComputeDiffers()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        (await AdvanceQualifierAsync(scenario, script)).IsError.ShouldBeFalse();
        var official = _harness.CreateRun(FactsReaderDoubles.Untouched());
        (await RunAsync(official, scenario, MakeOfficial(), needsFacts: false)).Value.OfficialStageIds
            .ShouldBe([scenario.Stage(1).Id, scenario.Stage(2).Id]);
        var versionBefore = (await StoredAsync(scenario)).Version;
        var firstTwo = script.QualifierPlaces.Take(2).ToList();
        script.QualifierPlaces = [firstTwo[1], firstTwo[0], .. script.QualifierPlaces.Skip(2)];
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        var result = await RunAsync(run, scenario, AdvanceFrom(scenario.Qualifier));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeTrue();
        result.Value.StaleStageIds.ShouldBe([scenario.Stage(1).Id]);
        result.Value.ComputedStageIds.ShouldBeEmpty();
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBeGreaterThan(versionBefore);
        var qualifying = stored.Stages.Single(stage => stage.Order == 1);
        qualifying.Status.ShouldBe(StageStatus.Official);
        qualifying.IsStale.ShouldBeTrue();
        stored.Stages.Single(stage => stage.Order == 2).IsStale.ShouldBeFalse();
        var places = stored.FieldEntries.ToDictionary(entry => entry.BowlerId, entry => entry.Place);
        places[firstTwo[0]].ShouldBe(1);
        places[firstTwo[1]].ShouldBe(2);
    }

    [Fact(DisplayName = "RunAsync should do nothing for an Official Stage whose dry compute gives the same output")]
    public async Task RunAsync_ShouldDoNothing_WhenOfficialStageDryComputeIsIdentical()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        (await AdvanceQualifierAsync(scenario, script)).IsError.ShouldBeFalse();
        var official = _harness.CreateRun(FactsReaderDoubles.Untouched());
        (await RunAsync(official, scenario, MakeOfficial(), needsFacts: false)).IsError.ShouldBeFalse();
        var versionBefore = (await StoredAsync(scenario)).Version;
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        var result = await RunAsync(run, scenario, AdvanceFrom(scenario.Qualifier));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeFalse();
        result.Value.StaleStageIds.ShouldBeEmpty();
        run.Saves.Saves.ShouldBe(0);
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBe(versionBefore);
        stored.Stages.Where(stage => stage.Order <= 2).ShouldAllBe(stage => stage.Status == StageStatus.Official && !stage.IsStale);
    }

    // Slot 8 goes to bowler 12, who is not a member, so it is vacant and the Backfill fills it. Bowler 9 is a member with
    // points who finished 9th, so the Backfill takes bowler 9. When the finish changes, bowler 9 holds slot 8 and the
    // Backfill has nothing to fill.
    [Fact(DisplayName = "RunAsync should move a bowler from a Backfill entry to a Qualifying entry in one save")]
    public async Task RunAsync_ShouldMoveBowlerFromBackfillToQualifying_InOneSave()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        var nonMember = scenario.Bowlers[11];
        var moving = scenario.Bowlers[8];
        script.SurvivorStatus = TournamentStatus.Scheduled;
        script.PlacementStatus = TournamentStatus.Scheduled;
        script.Members = scenario.Bowlers.Where(bowler => bowler != nonMember).ToHashSet();
        script.Backfill = BackfillFor(scenario, (moving, 50));
        script.QualifierPlaces = [.. scenario.Bowlers.Take(7), nonMember, moving];
        (await AdvanceQualifierAsync(scenario, script)).IsError.ShouldBeFalse();
        var firstRun = (await StoredAsync(scenario)).FieldEntries.Single(entry => entry.BowlerId == moving);
        firstRun.IsBackfill.ShouldBeTrue();
        script.QualifierPlaces = [.. scenario.Bowlers.Take(7), moving, nonMember];
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build));

        // Act
        var result = await RunAsync(run, scenario, AdvanceFrom(scenario.Qualifier));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ComputedStageIds.ShouldBe([scenario.Stage(1).Id]);
        run.Saves.Saves.ShouldBe(1);
        var stored = await StoredAsync(scenario);
        stored.FieldEntries.Count.ShouldBe(8);
        stored.FieldEntries.ShouldAllBe(entry => entry.SourceStageId == scenario.Stage(1).Id && entry.Place != null && !entry.IsBackfill);
        var entry = stored.FieldEntries.Single(e => e.BowlerId == moving);
        entry.Place.ShouldBe(8);
        entry.BackfillPoints.ShouldBeNull();
    }

    [Fact(DisplayName = "RunAsync should move a bowler from a Qualifying entry to a Backfill entry in one save")]
    public async Task RunAsync_ShouldMoveBowlerFromQualifyingToBackfill_InOneSave()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        var nonMember = scenario.Bowlers[11];
        var moving = scenario.Bowlers[7];
        script.SurvivorStatus = TournamentStatus.Scheduled;
        script.PlacementStatus = TournamentStatus.Scheduled;
        script.Members = scenario.Bowlers.Where(bowler => bowler != nonMember).ToHashSet();
        script.Backfill = BackfillFor(scenario, (moving, 40));
        script.QualifierPlaces = [.. scenario.Bowlers.Take(8)];
        (await AdvanceQualifierAsync(scenario, script)).IsError.ShouldBeFalse();
        (await StoredAsync(scenario)).FieldEntries.Single(entry => entry.BowlerId == moving).IsBackfill.ShouldBeFalse();
        script.QualifierPlaces = [.. scenario.Bowlers.Take(7), nonMember, moving];
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build));

        // Act
        var result = await RunAsync(run, scenario, AdvanceFrom(scenario.Qualifier));

        // Assert
        result.IsError.ShouldBeFalse();
        run.Saves.Saves.ShouldBe(1);
        var stored = await StoredAsync(scenario);
        stored.FieldEntries.Count.ShouldBe(8);
        var entry = stored.FieldEntries.Single(e => e.BowlerId == moving);
        entry.IsBackfill.ShouldBeTrue();
        entry.SourceStageId.ShouldBeNull();
        entry.Place.ShouldBeNull();
        entry.BackfillPoints.ShouldBe(40);
    }

    [Fact(DisplayName = "RunAsync should swap Survivor outcomes and Placement positions in place without a key clash")]
    public async Task RunAsync_ShouldSwapOutcomesAndPositionsInPlace()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        (await AdvanceQualifierAsync(scenario, script)).IsError.ShouldBeFalse();
        var first = scenario.Bowlers[0];
        var second = scenario.Bowlers[1];
        script.SurvivorRanking = [second, first, .. scenario.Bowlers.Skip(2).Take(7)];
        script.PlacementPlaces = [second, first, .. scenario.Bowlers.Skip(2).Take(7)];
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        var result = await RunAsync(run, scenario, AdvanceFrom(scenario.Survivor));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ComputedStageIds.ShouldBe([scenario.Stage(2).Id, scenario.Stage(3).Id]);
        run.Saves.Saves.ShouldBe(1);
        var stored = await StoredAsync(scenario);
        var outcomes = stored.Stages.Single(stage => stage.Order == 2).SurvivorOutcomes.ToDictionary(outcome => outcome.BowlerId);
        outcomes[second].BlockTotal.ShouldBe(1500);
        outcomes[first].BlockTotal.ShouldBe(1490);
        var positions = stored.Positions.ToDictionary(position => position.BowlerId);
        positions[second].Position.ShouldBe(1);
        positions[first].Position.ShouldBe(2);
        stored.Positions.Count.ShouldBe(8);
    }

    [Fact(DisplayName = "RunAsync should leave no partial output and release the lock when the save throws")]
    public async Task RunAsync_ShouldLeaveNoPartialOutput_WhenSaveThrows()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        var unsaved = BowlerId.New();
        script.Members = scenario.Bowlers.Append(unsaved).ToHashSet();
        script.QualifierPlaces = [unsaved, .. scenario.Bowlers.Skip(1).Take(7)];
        var (locks, handle) = KeyedLockDoubles.Granting(scenario.SeriesId, AdvancementRunHarness.Wait);
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), locks, SeriesCache.Untouched());

        // Act
        var act = () => RunAsync(run, scenario, AdvanceFrom(scenario.Qualifier));

        // Assert
        await Should.ThrowAsync<DbUpdateException>(act);
        handle.Verify(h => h.Dispose(), Times.Once);
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBe(0);
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Pending);
        stored.FieldEntries.ShouldBeEmpty();
        stored.Positions.ShouldBeEmpty();
        stored.Stages.SelectMany(stage => stage.SurvivorOutcomes).ShouldBeEmpty();
    }

    [Fact(DisplayName = "RunAsync should return the operation's error without saving the changes it made")]
    public async Task RunAsync_ShouldReturnOperationError_WithoutSaving()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        var failure = Error.Validation("Test.OperationFailed", "The operation failed.");
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        var result = await RunAsync(run, scenario, (series, _, _) =>
        {
            series.Rename("Changed before the error").IsError.ShouldBeFalse();

            return failure;
        });

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(failure);
        run.Saves.Saves.ShouldBe(0);
        (await StoredAsync(scenario)).Name.ShouldBe(scenario.Series.Name);
    }

    [Fact(DisplayName = "RunAsync should return NotFound without reading facts or running the operation when the Series is missing")]
    public async Task RunAsync_ShouldReturnNotFound_WhenSeriesIsMissing()
    {
        // Arrange
        var missing = SeriesId.New();
        var (locks, handle) = KeyedLockDoubles.Granting(missing, AdvancementRunHarness.Wait);
        var reader = FactsReaderDoubles.Untouched();
        var run = _harness.CreateRun(reader, locks, SeriesCache.Untouched());
        var operationCalls = 0;

        // Act
        var result = await run.Run.RunAsync(
            missing,
            (series, facts, now) =>
            {
                operationCalls++;

                return AdvanceResult.None;
            },
            AdvancementRunHarness.Wait,
            needsFacts: true,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesErrors.NotFound(missing));
        operationCalls.ShouldBe(0);
        handle.Verify(h => h.Dispose(), Times.Once);
    }

    [Fact(DisplayName = "RunAsync should pass empty facts and never call the facts reader when needsFacts is false")]
    public async Task RunAsync_ShouldNotReadFacts_WhenNeedsFactsIsFalse()
    {
        // Arrange
        var (scenario, _) = await SeedAsync();
        var reader = FactsReaderDoubles.Untouched();
        var run = _harness.CreateRun(reader, cache: SeriesCache.Untouched());
        AdvancementFacts? received = null;

        // Act
        var result = await RunAsync(run, scenario, (_, facts, _) =>
        {
            received = facts;

            return AdvanceResult.None;
        }, needsFacts: false);

        // Assert
        result.IsError.ShouldBeFalse();
        received.ShouldBeSameAs(AdvancementFacts.Empty);
        FactsReaderDoubles.VerifyReadTimes(reader, Times.Never);
    }

    [Fact(DisplayName = "RunAsync should read the facts once and pass them to the operation when needsFacts is true")]
    public async Task RunAsync_ShouldReadFactsOnce_WhenNeedsFactsIsTrue()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        var facts = script.Build();
        var reader = FactsReaderDoubles.Reading(() => facts);
        var run = _harness.CreateRun(reader, cache: SeriesCache.Untouched());
        AdvancementFacts? received = null;

        // Act
        var result = await RunAsync(run, scenario, (_, given, _) =>
        {
            received = given;

            return AdvanceResult.None;
        });

        // Assert
        result.IsError.ShouldBeFalse();
        received.ShouldBeSameAs(facts);
        FactsReaderDoubles.VerifyReadTimes(reader, Times.Once);
    }

    [Fact(DisplayName = "RunForTournamentAsync should do nothing for a tournament that is in no Series")]
    public async Task RunForTournamentAsync_ShouldDoNothing_WhenTournamentIsInNoSeries()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (scenario, _) = await SeedAsync();
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2026, ct);
        var unlinked = await SeriesSeed.AddTournamentAsync(_dbContext, season, "Not in a Series", new DateOnly(2026, 2, 1), null, ct);
        var run = _harness.CreateRun(
            FactsReaderDoubles.Untouched(), KeyedLockDoubles.Untouched(), SeriesCache.Untouched());
        var operationCalls = 0;

        // Act
        await run.Run.RunForTournamentAsync(
            unlinked.Id,
            (_, _, _) =>
            {
                operationCalls++;

                return AdvanceResult.None;
            },
            ct);

        // Assert
        operationCalls.ShouldBe(0);
        run.Saves.Saves.ShouldBe(0);
        (await StoredAsync(scenario)).Version.ShouldBe(0);
    }

    [Fact(DisplayName = "RunForTournamentAsync should run the operation once under the Series' lock for a tournament in a Series")]
    public async Task RunForTournamentAsync_ShouldRunOnce_WhenTournamentIsInASeries()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        var (locks, handle) = KeyedLockDoubles.Granting(scenario.SeriesId, SeriesAdvancement.JobLockWait);
        var reader = FactsReaderDoubles.Reading(script.Build);
        var run = _harness.CreateRun(reader, locks);
        var operationCalls = 0;

        // Act
        await run.Run.RunForTournamentAsync(
            scenario.Qualifier.Id,
            (series, facts, now) =>
            {
                operationCalls++;

                return series.Advance(scenario.Qualifier.Id, facts, now);
            },
            TestContext.Current.CancellationToken);

        // Assert
        operationCalls.ShouldBe(1);
        FactsReaderDoubles.VerifyReadTimes(reader, Times.Once);
        handle.Verify(h => h.Dispose(), Times.Once);
        (await StoredAsync(scenario)).Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed);
    }

    [Fact(DisplayName = "RunForTournamentAsync should throw so the job retries when the Series lock is busy")]
    public async Task RunForTournamentAsync_ShouldThrow_WhenLockIsBusy()
    {
        // Arrange
        var (scenario, _) = await SeedAsync();
        var run = _harness.CreateRun(
            FactsReaderDoubles.Untouched(),
            KeyedLockDoubles.Refusing(scenario.SeriesId, SeriesAdvancement.JobLockWait),
            SeriesCache.Untouched());

        // Act
        var act = () => run.Run.RunForTournamentAsync(
            scenario.Qualifier.Id, (_, _, _) => AdvanceResult.None, TestContext.Current.CancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("Series.Advancement.Busy");
    }

    [Theory(DisplayName = "EnsureJobSucceeded should throw InvalidOperationException for a Busy, ConcurrentChange or Validation error")]
    [InlineData("Busy")]
    [InlineData("ConcurrentChange")]
    [InlineData("Validation")]
    public void EnsureJobSucceeded_ShouldThrow_WhenResultIsARetryableOrInvalidError(string kind)
    {
        // Arrange
        var error = kind switch
        {
            "Busy" => SeriesAdvancementErrors.Busy,
            "ConcurrentChange" => SeriesAdvancementErrors.ConcurrentChange,
            _ => SeriesAdvancementErrors.NoResultRow,
        };
        ErrorOr<AdvanceResult> result = error;

        // Act
        var act = () => SeriesAdvancement.EnsureJobSucceeded(result);

        // Assert
        var exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldContain(error.Code);
    }

    [Fact(DisplayName = "EnsureJobSucceeded should ignore NotFound because a deleted Series needs no advancement")]
    public void EnsureJobSucceeded_ShouldIgnoreNotFound()
    {
        // Arrange
        ErrorOr<AdvanceResult> result = SeriesErrors.NotFound(SeriesId.New());

        // Act
        var act = () => SeriesAdvancement.EnsureJobSucceeded(result);

        // Assert
        Should.NotThrow(act);
    }

    [Fact(DisplayName = "EnsureJobSucceeded should accept a successful result, changed or not")]
    public void EnsureJobSucceeded_ShouldAcceptSuccess()
    {
        // Arrange
        ErrorOr<AdvanceResult> unchanged = AdvanceResult.None;
        ErrorOr<AdvanceResult> changed = new AdvanceResult { Changed = true, Version = 3 };

        // Act
        var act = () =>
        {
            SeriesAdvancement.EnsureJobSucceeded(unchanged);
            SeriesAdvancement.EnsureJobSucceeded(changed);
        };

        // Assert
        Should.NotThrow(act);
    }

    private static BackfillFacts BackfillFor(AdvancementScenario scenario, params (BowlerId Bowler, int Points)[] points)
        => new(scenario.Qualifier.EndDate, true, [.. points.Select(p => new BowlerPointsFact(p.Bowler, p.Points))]);
}