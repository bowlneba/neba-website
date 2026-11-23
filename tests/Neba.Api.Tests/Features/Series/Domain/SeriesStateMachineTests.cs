using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class SeriesStateMachineTests
{
    private static readonly DateTimeOffset Now = AdvancementHarness.Now;
    private static readonly TimeSpan Grace = AdvancementHarness.Grace;

    // ---- Pending to Computed to Official ----

    [Fact(DisplayName = "Advance should move a Stage from Pending to Computed and stamp ComputedAt")]
    public void Advance_ShouldMoveStageToComputed()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying);
        var stage = harness.Stage(1);

        // Act
        var result = harness.Advance();

        // Assert
        stage.Status.ShouldBe(StageStatus.Computed);
        stage.ComputedAt.ShouldBe(Now);
        stage.OfficialAt.ShouldBeNull();
        stage.IsStale.ShouldBeFalse();
        harness.Series.IsLocked.ShouldBeTrue();
        result.ComputedStageIds.ShouldBe([stage.Id]);
        result.StatusChanged.ShouldBeTrue();
    }

    [Fact(DisplayName = "MakeDueOfficial should move a Computed Stage to Official and stamp OfficialAt")]
    public void MakeDueOfficial_ShouldMoveStageToOfficial()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();
        var stage = harness.Stage(1);

        // Act
        var result = harness.MakeDueOfficial();

        // Assert
        stage.Status.ShouldBe(StageStatus.Official);
        stage.OfficialAt.ShouldBe(Now + Grace);
        result.OfficialStageIds.ShouldBe([stage.Id]);
        result.StatusChanged.ShouldBeTrue();
    }

    [Fact(DisplayName = "MakeDueOfficial should hold back a later Stage while an earlier Stage is Stale")]
    public void MakeDueOfficial_ShouldHoldBackLaterStage_WhenEarlierStageIsStale()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Survivor, StageKind.Placement).Advanced();
        harness.SetResults(1, []);
        _ = harness.Advance(now: Now.AddHours(1));
        harness.Stage(1).IsStale.ShouldBeTrue();
        harness.Stage(2).Status.ShouldBe(StageStatus.Computed);

        // Act
        var result = harness.MakeDueOfficial(Now.AddDays(10));

        // Assert
        harness.Stage(1).Status.ShouldBe(StageStatus.Computed);
        harness.Stage(2).Status.ShouldBe(StageStatus.Computed);
        result.OfficialStageIds.ShouldBeEmpty();
    }

    [Theory(DisplayName = "Advance should leave a Stage Pending unless its tournament is Completed or Truncated")]
    [InlineData("Scheduled", false)]
    [InlineData("Cancelled", false)]
    [InlineData("Completed", true)]
    [InlineData("Truncated", true)]
    public void Advance_ShouldComputeOnlyForFinishedTournaments(string status, bool computes)
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).SetStatus(1, TournamentStatus.FromName(status));

        // Act
        var result = harness.Advance();

        // Assert
        harness.Stage(1).Status.ShouldBe(computes ? StageStatus.Computed : StageStatus.Pending);
        result.Changed.ShouldBe(computes);
    }

    [Fact(DisplayName = "Advance should leave a Stage Pending when its tournament has no result rows")]
    public void Advance_ShouldLeaveStagePending_WhenNoResultRows()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).SetResults(1, []);

        // Act
        var result = harness.Advance();

        // Assert
        harness.Stage(1).Status.ShouldBe(StageStatus.Pending);
        result.Changed.ShouldBeFalse();
        harness.Series.Version.ShouldBe(0);
    }

    [Fact(DisplayName = "Advance should leave a Stage Pending until the previous Stage is computed")]
    public void Advance_ShouldLeaveStagePending_WhenPreviousStageIsPending()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Qualifying)
            .SetStatus(1, TournamentStatus.Scheduled);

        // Act
        var result = harness.Advance(2);

        // Assert
        harness.Stage(1).Status.ShouldBe(StageStatus.Pending);
        harness.Stage(2).Status.ShouldBe(StageStatus.Pending);
        result.Changed.ShouldBeFalse();
        harness.Series.FieldEntries.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Advance should compute every later Stage that can compute in one pass")]
    public void Advance_ShouldComputeLaterStages_InOnePass()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Qualifying, StageKind.Placement);

        // Act
        var result = harness.Advance();

        // Assert
        harness.Series.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed);
        result.ComputedStageIds.Count.ShouldBe(3);
        harness.Series.FieldEntries.Count.ShouldBe(2 * AdvancementHarness.SlotCount);
        harness.Series.Positions.Count.ShouldBe(2 * AdvancementHarness.SlotCount);
    }

    [Theory(DisplayName = "Advance should never return a Stage to Pending when its results disappear, and mark it Stale instead")]
    [InlineData(false)]
    [InlineData(true)]
    public void Advance_ShouldMarkStale_WhenResultsDisappear(bool official)
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();
        if (official)
        {
            _ = harness.MakeDueOfficial();
        }

        var statusBefore = harness.Stage(1).Status;
        harness.SetResults(1, []);

        // Act
        var result = harness.Advance(now: Now.AddDays(3));

        // Assert
        harness.Stage(1).Status.ShouldBe(statusBefore);
        harness.Stage(1).IsStale.ShouldBeTrue();
        harness.Series.FieldEntries.Count.ShouldBe(AdvancementHarness.SlotCount);
        result.StaleStageIds.ShouldBe([harness.Stage(1).Id]);
    }

    // ---- Official ----

    [Fact(DisplayName = "Advance should ignore an event for an Official Stage when a dry compute matches")]
    public void Advance_ShouldIgnoreEvent_WhenOfficialOutputMatches()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();
        _ = harness.MakeDueOfficial();
        var version = harness.Series.Version;

        // Act
        var result = harness.Advance(now: Now.AddDays(3));

        // Assert
        result.Changed.ShouldBeFalse();
        harness.Stage(1).Status.ShouldBe(StageStatus.Official);
        harness.Stage(1).IsStale.ShouldBeFalse();
        harness.Series.Version.ShouldBe(version);
    }

    [Fact(DisplayName = "Advance should mark an Official Stage Stale, not rewrite it, when a dry compute differs")]
    public void Advance_ShouldMarkOfficialStageStale_WhenDryComputeDiffers()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();
        _ = harness.MakeDueOfficial();
        var first = harness.Qualifiers(1)[0];
        harness.SwapFirstTwoPlaces(1);

        // Act
        var result = harness.Advance(now: Now.AddDays(3));

        // Assert
        var stage = harness.Stage(1);
        stage.Status.ShouldBe(StageStatus.Official);
        stage.IsStale.ShouldBeTrue();
        stage.OfficialAt.ShouldBe(Now + Grace);
        stage.ComputedAt.ShouldBe(Now);
        harness.Series.FieldEntries.Single(entry => entry.BowlerId == first).Place.ShouldBe(1);
        result.StaleStageIds.ShouldBe([stage.Id]);
        result.Changed.ShouldBeTrue();
        result.StatusChanged.ShouldBeFalse();
    }

    [Fact(DisplayName = "Advance should not change an Official Stage again once it is Stale")]
    public void Advance_ShouldStayQuiet_WhenOfficialStageIsAlreadyStale()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();
        _ = harness.MakeDueOfficial();
        harness.SwapFirstTwoPlaces(1);
        _ = harness.Advance(now: Now.AddDays(3));
        var version = harness.Series.Version;

        // Act
        var result = harness.Advance(now: Now.AddDays(4));

        // Assert
        result.Changed.ShouldBeFalse();
        harness.Series.Version.ShouldBe(version);
    }

    // ---- forced Recompute ----

    [Fact(DisplayName = "Recompute should require confirmation when an Official Stage is in range and change nothing")]
    public void Recompute_ShouldRequireConfirmation_WhenAnOfficialStageIsInRange()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();
        _ = harness.MakeDueOfficial();
        var version = harness.Series.Version;

        // Act
        var result = harness.Recompute(1, confirm: false);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.Recompute.ConfirmationRequired");
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
        ((int[])result.FirstError.Metadata![SeriesAdvancementErrors.OfficialStageOrdersKey]).ShouldBe([1]);
        harness.Stage(1).Status.ShouldBe(StageStatus.Official);
        harness.Series.Version.ShouldBe(version);
    }

    [Fact(DisplayName = "Recompute should list every Official Stage from the chosen Stage on")]
    public void Recompute_ShouldListOfficialStagesInRange()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Qualifying).Advanced();
        _ = harness.MakeDueOfficial();

        // Act
        var fromFirst = harness.Recompute(1, confirm: false);
        var fromSecond = harness.Recompute(2, confirm: false);

        // Assert
        ((int[])fromFirst.FirstError.Metadata![SeriesAdvancementErrors.OfficialStageOrdersKey]).ShouldBe([1, 2]);
        ((int[])fromSecond.FirstError.Metadata![SeriesAdvancementErrors.OfficialStageOrdersKey]).ShouldBe([2]);
    }

    [Fact(DisplayName = "Recompute should not need confirmation when no Official Stage is in range")]
    public void Recompute_ShouldNotNeedConfirmation_WhenNoOfficialStageIsInRange()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Qualifying)
            .SetStatus(2, TournamentStatus.Scheduled)
            .Advanced();
        _ = harness.MakeDueOfficial();

        // Act
        var result = harness.Recompute(2, confirm: false);

        // Assert
        result.IsError.ShouldBeFalse();
        harness.Stage(1).Status.ShouldBe(StageStatus.Official);
        harness.Stage(2).Status.ShouldBe(StageStatus.Pending);
    }

    [Fact(DisplayName = "Recompute with confirmation should reset an Official Stale Stage to a fresh Computed Stage")]
    public void Recompute_ShouldResetOfficialAtStaleAndComputed_WhenConfirmed()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();
        _ = harness.MakeDueOfficial();
        var first = harness.Qualifiers(1)[0];
        harness.SwapFirstTwoPlaces(1);
        _ = harness.Advance(now: Now.AddDays(3));
        var later = Now.AddDays(5);

        // Act
        var result = harness.Recompute(1, confirm: true, now: later);

        // Assert
        var stage = harness.Stage(1);
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeTrue();
        result.Value.StatusChanged.ShouldBeTrue();
        stage.Status.ShouldBe(StageStatus.Computed);
        stage.OfficialAt.ShouldBeNull();
        stage.IsStale.ShouldBeFalse();
        stage.ComputedAt.ShouldBe(later);
        harness.Series.FieldEntries.Single(entry => entry.BowlerId == first).Place.ShouldBe(2);
    }

    [Fact(DisplayName = "Recompute with confirmation should reset an Official Stage to Computed even when the output is the same")]
    public void Recompute_ShouldResetOfficialStage_WhenOutputIsTheSame()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();
        _ = harness.MakeDueOfficial();
        var version = harness.Series.Version;

        // Act
        var result = harness.Recompute(1, confirm: true, now: Now.AddDays(5));

        // Assert
        result.IsError.ShouldBeFalse();
        harness.Stage(1).Status.ShouldBe(StageStatus.Computed);
        harness.Stage(1).OfficialAt.ShouldBeNull();
        harness.Series.Version.ShouldBe(version + 1);
    }

    [Fact(DisplayName = "Recompute with confirmation should reset an Official Stage that can no longer compute to a Stale Computed Stage")]
    public void Recompute_ShouldResetOfficialStage_WhenItCanNoLongerCompute()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();
        _ = harness.MakeDueOfficial();
        harness.SetResults(1, []);
        var later = Now.AddDays(5);

        // Act
        var result = harness.Recompute(1, confirm: true, now: later);

        // Assert
        var stage = harness.Stage(1);
        result.IsError.ShouldBeFalse();
        result.Value.StatusChanged.ShouldBeTrue();
        stage.Status.ShouldBe(StageStatus.Computed);
        stage.OfficialAt.ShouldBeNull();
        stage.IsStale.ShouldBeTrue();
        result.Value.StaleStageIds.ShouldBe([stage.Id]);
    }

    [Fact(DisplayName = "Recompute should clear Stale on a Computed Stage whose output matches again")]
    public void Recompute_ShouldClearStale_WhenComputedOutputMatchesAgain()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();
        var results = harness.Tournament(1).Results;
        harness.SetResults(1, []);
        _ = harness.Advance(now: Now.AddHours(1));
        harness.SetResults(1, results);

        // Act
        var result = harness.Recompute(1);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeTrue();
        harness.Stage(1).IsStale.ShouldBeFalse();
        harness.Stage(1).Status.ShouldBe(StageStatus.Computed);
    }

    [Fact(DisplayName = "Recompute should return NotFound for a Stage the Series does not have")]
    public void Recompute_ShouldReturnNotFound_WhenStageIsUnknown()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();

        // Act
        var result = harness.Series.Recompute(SeriesStageId.New(), false, harness.Series.Version, harness.Facts, Now);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Advancement.StageNotFound");
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact(DisplayName = "Recompute should return a version conflict before it asks for confirmation")]
    public void Recompute_ShouldReturnVersionConflict_WhenVersionIsStale()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();
        _ = harness.MakeDueOfficial();

        // Act
        var result = harness.Recompute(1, confirm: false, version: harness.Series.Version - 1);

        // Assert
        result.FirstError.ShouldBe(SeriesAdvancementErrors.VersionConflict);
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
    }

    // ---- MakeDueOfficial ----

    [Fact(DisplayName = "MakeDueOfficial should wait until the grace period has passed")]
    public void MakeDueOfficial_ShouldWaitForTheGracePeriod()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();

        // Act
        var early = harness.MakeDueOfficial(Now + Grace - TimeSpan.FromTicks(1));
        var onTime = harness.MakeDueOfficial(Now + Grace);

        // Assert
        early.OfficialStageIds.ShouldBeEmpty();
        onTime.OfficialStageIds.ShouldBe([harness.Stage(1).Id]);
    }

    [Fact(DisplayName = "MakeDueOfficial should make Qualifying and Survivor Stages Official in order")]
    public void MakeDueOfficial_ShouldMakeQualifyingAndSurvivorStagesOfficial()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Survivor, StageKind.Survivor).Advanced();

        // Act
        var result = harness.MakeDueOfficial();

        // Assert
        result.OfficialStageIds.ShouldBe([harness.Stage(1).Id, harness.Stage(2).Id, harness.Stage(3).Id]);
        harness.Series.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Official);
    }

    [Fact(DisplayName = "MakeDueOfficial should wait for an earlier Stage whose grace period has not ended")]
    public void MakeDueOfficial_ShouldWaitForEarlierStages()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Qualifying).Advanced();
        harness.SwapFirstTwoPlaces(1);
        _ = harness.Advance(now: Now.AddHours(20)); // Stage 1 recomputed later; Stage 2 keeps its earlier ComputedAt

        // Act
        var beforeFirstIsDue = harness.MakeDueOfficial(Now.AddHours(25));
        var afterBothAreDue = harness.MakeDueOfficial(Now.AddHours(45));

        // Assert
        beforeFirstIsDue.OfficialStageIds.ShouldBeEmpty();
        afterBothAreDue.OfficialStageIds.ShouldBe([harness.Stage(1).Id, harness.Stage(2).Id]);
    }

    [Fact(DisplayName = "MakeDueOfficial should skip a Stale Stage and hold every Stage after it")]
    public void MakeDueOfficial_ShouldSkipStaleStages()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Qualifying).Advanced();
        harness.SetResults(1, []);
        _ = harness.Advance(now: Now.AddHours(1));

        // Act
        var result = harness.MakeDueOfficial(Now.AddDays(10));

        // Assert
        harness.Stage(1).IsStale.ShouldBeTrue();
        result.Changed.ShouldBeFalse();
        harness.Series.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed);
    }

    [Fact(DisplayName = "MakeDueOfficial should hold the last Qualifying Stage until the Backfill ran")]
    public void MakeDueOfficial_ShouldHoldLastQualifyingStage_UntilBackfillRan()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying);
        var replacement = BowlerId.New();
        var members = harness.Qualifiers(1).Skip(1).Append(replacement).ToList(); // the place 1 bowler is not a member
        var points = new[] { BowlerPointsFactFactory.Create(replacement, 50) };
        harness.SetMembers(1, members).Advanced();
        var stage = harness.Stage(1);

        // Act
        var withoutBackfill = harness.MakeDueOfficial(Now.AddDays(10));
        var dueAtBefore = harness.Series.OfficialDueAt(stage, Grace);
        harness.SetBackfill(BackfillFactsFactory.Create(harness.EndDate(1), ready: false, points: points));
        _ = harness.Advance(now: Now.AddHours(1));
        var notReady = harness.MakeDueOfficial(Now.AddDays(10));
        harness.SetBackfill(BackfillFactsFactory.Create(harness.EndDate(1), ready: true, points: points));
        _ = harness.Advance(now: Now.AddHours(2));
        var dueAtAfter = harness.Series.OfficialDueAt(stage, Grace);
        var ready = harness.MakeDueOfficial(Now.AddHours(2) + Grace);

        // Assert
        withoutBackfill.Changed.ShouldBeFalse();
        dueAtBefore.ShouldBeNull();
        notReady.Changed.ShouldBeFalse();
        dueAtAfter.ShouldBe(Now.AddHours(2) + Grace);
        ready.OfficialStageIds.ShouldBe([stage.Id]);
        harness.Series.BackfillAsOf.ShouldBe(harness.EndDate(1));
        harness.Series.FieldEntries.Single(entry => entry.IsBackfill).BowlerId.ShouldBe(replacement);
    }

    [Fact(DisplayName = "MakeDueOfficial should not hold an earlier Qualifying Stage for the Backfill")]
    public void MakeDueOfficial_ShouldNotHoldEarlierQualifyingStage()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Qualifying)
            .SetStatus(2, TournamentStatus.Scheduled)
            .Advanced();

        // Act
        var result = harness.MakeDueOfficial();

        // Assert
        result.OfficialStageIds.ShouldBe([harness.Stage(1).Id]);
        harness.Stage(2).Status.ShouldBe(StageStatus.Pending);
    }

    [Fact(DisplayName = "MakeDueOfficial should never make a Placement Stage Official")]
    public void MakeDueOfficial_ShouldNeverMakePlacementOfficial()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Placement).Advanced();
        var placement = harness.Stage(2);

        // Act
        var result = harness.MakeDueOfficial(Now.AddDays(365));

        // Assert
        result.OfficialStageIds.ShouldBe([harness.Stage(1).Id]);
        placement.Status.ShouldBe(StageStatus.Computed);
        harness.Series.OfficialDueAt(placement, Grace).ShouldBeNull();
    }

    // ---- LastQualifyingStage ----

    [Fact(DisplayName = "LastQualifyingStage should be the Qualifying Stage with the highest order, or null without one")]
    public void LastQualifyingStage_ShouldBeTheLastQualifyingStage()
    {
        // Arrange
        var withQualifying = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Qualifying, StageKind.Survivor);
        var withoutQualifying = AdvancementHarness.Create(StageKind.Placement);

        // Act
        var last = withQualifying.Series.LastQualifyingStage;
        var none = withoutQualifying.Series.LastQualifyingStage;

        // Assert
        last.ShouldBe(withQualifying.Stage(2));
        none.ShouldBeNull();
    }
}