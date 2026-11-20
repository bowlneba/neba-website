using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class SeriesOverrideTests
{
    private static readonly DateTimeOffset Now = AdvancementHarness.Now;

    // A Computed Qualifying Stage: eight members in places 1 to 8, plus one non-member with a result row in place 9.
    private static (AdvancementHarness Harness, BowlerId Extra) Computed(params StageKind[] kinds)
    {
        var harness = AdvancementHarness.Create(kinds.Length == 0 ? [StageKind.Qualifying] : kinds);
        var extra = BowlerId.New();
        harness.SetResults(1, [.. harness.Tournament(1).Results, StageResultRowFactory.Create(extra, AdvancementHarness.SlotCount + 1)]);
        _ = harness.Advance();

        return (harness, extra);
    }

    private static BackfillFacts Ready(AdvancementHarness harness, int order, params BowlerPointsFact[] points)
        => BackfillFactsFactory.Create(harness.EndDate(order), ready: true, points: points);

    // ---- behavior ----

    [Fact(DisplayName = "AddOverride should put a bowler with a result row in the field, past the slot count")]
    public void AddOverride_ShouldAddBowlerToField()
    {
        // Arrange
        var (harness, extra) = Computed();
        var at = Now.AddHours(3);

        // Act
        var result = harness.AddOverride(1, extra, "  Bowled the qualifier.  ", now: at);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeTrue();
        result.Value.ComputedStageIds.ShouldBe([harness.Stage(1).Id]);
        var created = harness.Stage(1).Overrides.Single();
        result.Value.OverrideId.ShouldBe(created.Id);
        created.Kind.ShouldBe(OverrideKind.Add);
        created.BowlerId.ShouldBe(extra);
        created.Reason.ShouldBe("Bowled the qualifier.");
        created.CreatedBy.ShouldBe("admin-1");
        created.CreatedAt.ShouldBe(at);
        created.Applied.ShouldBeTrue();
        var entry = harness.Series.FieldEntries.Single(e => e.BowlerId == extra);
        entry.ViaOverride.ShouldBeTrue();
        entry.Place.ShouldBe(AdvancementHarness.SlotCount + 1);
        entry.SourceStageId.ShouldBe(harness.Stage(1).Id);
        harness.Series.FieldEntries.Count.ShouldBe(AdvancementHarness.SlotCount + 1);
        harness.Stage(1).SlotCount.ShouldBe(AdvancementHarness.SlotCount);
    }

    [Fact(DisplayName = "ClearSlot should vacate the slot a bowler holds and keep the slot count")]
    public void ClearSlot_ShouldVacateSlot()
    {
        // Arrange
        var (harness, _) = Computed();
        var cleared = harness.Qualifiers(1)[0];

        // Act
        var result = harness.ClearSlot(1, cleared);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.OverrideId.ShouldBe(harness.Stage(1).Overrides.Single().Id);
        harness.Stage(1).Overrides.Single().Kind.ShouldBe(OverrideKind.Clear);
        harness.Stage(1).Overrides.Single().Applied.ShouldBeTrue();
        harness.Series.FieldEntries.ShouldNotContain(e => e.BowlerId == cleared);
        harness.Series.FieldEntries.Count.ShouldBe(AdvancementHarness.SlotCount - 1);
        harness.Stage(1).SlotCount.ShouldBe(AdvancementHarness.SlotCount);
    }

    [Fact(DisplayName = "ClearSlot should bar the cleared bowler from the Backfill and let the next bowler take the slot")]
    public void ClearSlot_ShouldBarBowlerFromBackfill()
    {
        // Arrange
        var (harness, _) = Computed();
        var cleared = harness.Qualifiers(1)[0];
        var replacement = BowlerId.New();
        harness.SetMembers(1, [.. harness.Qualifiers(1), replacement])
            .SetBackfill(Ready(
                harness,
                1,
                BowlerPointsFactFactory.Create(cleared, 500),
                BowlerPointsFactFactory.Create(replacement, 100)));

        // Act
        var result = harness.ClearSlot(1, cleared);

        // Assert
        result.IsError.ShouldBeFalse();
        harness.Series.FieldEntries.ShouldNotContain(e => e.BowlerId == cleared);
        var backfill = harness.Series.FieldEntries.Single(e => e.IsBackfill);
        backfill.BowlerId.ShouldBe(replacement);
        backfill.BackfillPoints.ShouldBe(100);
    }

    [Fact(DisplayName = "A Clear in an earlier Qualifying Stage should bar the bowler from the Backfill in the last Stage")]
    public void ClearSlot_ShouldBarBowlerFromBackfill_WhenClearedInAnEarlierStage()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Qualifying).Advanced();
        var cleared = harness.Qualifiers(1)[0];
        var replacement = BowlerId.New();
        harness.SetMembers(2, [.. harness.Qualifiers(2), cleared, replacement])
            .SetBackfill(Ready(
                harness,
                2,
                BowlerPointsFactFactory.Create(cleared, 500),
                BowlerPointsFactFactory.Create(replacement, 100)));

        // Act
        var result = harness.ClearSlot(1, cleared);

        // Assert
        result.IsError.ShouldBeFalse();
        harness.Series.FieldEntries.ShouldNotContain(e => e.BowlerId == cleared);
        harness.Series.FieldEntries.Single(e => e.IsBackfill).BowlerId.ShouldBe(replacement);
    }

    [Fact(DisplayName = "Backfill should not bar a bowler whose Clear no longer applies")]
    public void Advance_ShouldNotBarBowler_WhenClearNoLongerApplies()
    {
        // Arrange
        var (harness, _) = Computed();
        var cleared = harness.Qualifiers(1)[0];
        var nonMember = harness.Qualifiers(1)[4];
        var other = BowlerId.New();
        _ = harness.ClearSlot(1, cleared);
        harness.SetResults(1, harness.Tournament(1).Results.Select(row => row.BowlerId == cleared ? row with { Place = 20 } : row))
            .SetMembers(1, [.. harness.Qualifiers(1).Where(b => b != nonMember), other])
            .SetBackfill(Ready(
                harness,
                1,
                BowlerPointsFactFactory.Create(cleared, 500),
                BowlerPointsFactFactory.Create(other, 100)));

        // Act
        _ = harness.Advance(now: Now.AddDays(1));

        // Assert
        harness.Stage(1).Overrides.Single().Applied.ShouldBeFalse();
        harness.Series.FieldEntries.Single(e => e.IsBackfill).BowlerId.ShouldBe(cleared);
    }

    [Fact(DisplayName = "RemoveOverride should return a cleared bowler to the field")]
    public void RemoveOverride_ShouldRestoreClearedBowler()
    {
        // Arrange
        var (harness, _) = Computed();
        var cleared = harness.Qualifiers(1)[0];
        var overrideId = harness.ClearSlot(1, cleared).Value.OverrideId!.Value;

        // Act
        var result = harness.RemoveOverride(1, overrideId);

        // Assert
        result.IsError.ShouldBeFalse();
        harness.Stage(1).Overrides.ShouldBeEmpty();
        harness.Series.FieldEntries.Single(e => e.BowlerId == cleared).Place.ShouldBe(1);
        harness.Series.FieldEntries.Count.ShouldBe(AdvancementHarness.SlotCount);
    }

    [Fact(DisplayName = "RemoveOverride should take an added bowler out of the field")]
    public void RemoveOverride_ShouldRemoveAddedBowler()
    {
        // Arrange
        var (harness, extra) = Computed();
        var overrideId = harness.AddOverride(1, extra).Value.OverrideId!.Value;

        // Act
        var result = harness.RemoveOverride(1, overrideId);

        // Assert
        result.IsError.ShouldBeFalse();
        harness.Series.FieldEntries.ShouldNotContain(e => e.BowlerId == extra);
        harness.Stage(1).Overrides.ShouldBeEmpty();
    }

    [Fact(DisplayName = "An override should survive a forced Recompute")]
    public void Recompute_ShouldKeepOverrides()
    {
        // Arrange
        var (harness, extra) = Computed();
        _ = harness.AddOverride(1, extra);

        // Act
        var result = harness.Recompute(1);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeFalse();
        harness.Stage(1).Overrides.Single().Applied.ShouldBeTrue();
        harness.Series.FieldEntries.ShouldContain(e => e.BowlerId == extra && e.ViaOverride);
    }

    // ---- errors ----

    [Fact(DisplayName = "AddOverride should return NotFound for a Stage the Series does not have")]
    public void AddOverride_ShouldReturnNotFound_WhenStageIsUnknown()
    {
        // Arrange
        var (harness, extra) = Computed();

        // Act
        var result = harness.Series.AddOverride(
            SeriesStageId.New(), extra, "Reason.", "admin-1", harness.Series.Version, harness.Facts, Now);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Advancement.StageNotFound");
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Theory(DisplayName = "AddOverride and ClearSlot should return StageNotQualifying for a Survivor Stage")]
    [InlineData(false)]
    [InlineData(true)]
    public void Override_ShouldReturnStageNotQualifying_WhenStageIsSurvivor(bool clear)
    {
        // Arrange
        var (harness, _) = Computed(StageKind.Qualifying, StageKind.Survivor);
        var bowler = harness.Qualifiers(1)[0];

        // Act
        var result = clear ? harness.ClearSlot(2, bowler) : harness.AddOverride(2, bowler);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Override.StageNotQualifying");
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact(DisplayName = "AddOverride should return StageNotComputed with the not-computed message for a Pending Stage")]
    public void AddOverride_ShouldReturnStageNotComputed_WhenStageIsPending()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying);

        // Act
        var result = harness.AddOverride(1, harness.Qualifiers(1)[0]);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Override.StageNotComputed");
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
        result.FirstError.Description.ShouldContain("not been computed");
    }

    [Theory(DisplayName = "AddOverride and ClearSlot should return StageNotComputed with the official message for an Official Stage")]
    [InlineData(false)]
    [InlineData(true)]
    public void Override_ShouldReturnStageNotComputed_WhenStageIsOfficial(bool clear)
    {
        // Arrange
        var (harness, extra) = Computed();
        _ = harness.MakeDueOfficial();

        // Act
        var result = clear ? harness.ClearSlot(1, harness.Qualifiers(1)[0]) : harness.AddOverride(1, extra);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Override.StageNotComputed");
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
        result.FirstError.Description.ShouldContain("official");
        harness.Stage(1).Overrides.ShouldBeEmpty();
    }

    [Fact(DisplayName = "AddOverride should return NoResultRow for a bowler with no result in the tournament")]
    public void AddOverride_ShouldReturnNoResultRow_WhenBowlerHasNoResult()
    {
        // Arrange
        var (harness, _) = Computed();

        // Act
        var result = harness.AddOverride(1, BowlerId.New());

        // Assert
        result.FirstError.Code.ShouldBe("Series.Override.NoResultRow");
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        harness.Stage(1).Overrides.ShouldBeEmpty();
    }

    [Fact(DisplayName = "AddOverride should return NoResultRow when the facts hold no tournament for the Stage")]
    public void AddOverride_ShouldReturnNoResultRow_WhenTournamentIsMissing()
    {
        // Arrange
        var (harness, extra) = Computed();

        // Act
        var result = harness.Series.AddOverride(
            harness.Stage(1).Id, extra, "Reason.", "admin-1", harness.Series.Version, AdvancementFacts.Empty, Now);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Override.NoResultRow");
    }

    [Fact(DisplayName = "AddOverride should return BowlerAlreadyInField for a bowler who already holds a slot")]
    public void AddOverride_ShouldReturnBowlerAlreadyInField_WhenBowlerIsQualified()
    {
        // Arrange
        var (harness, _) = Computed();

        // Act
        var result = harness.AddOverride(1, harness.Qualifiers(1)[0]);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Override.BowlerAlreadyInField");
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
        harness.Stage(1).Overrides.ShouldBeEmpty();
    }

    [Fact(DisplayName = "AddOverride should return Exists for a bowler who already has a Clear on the Stage")]
    public void AddOverride_ShouldReturnExists_WhenBowlerWasCleared()
    {
        // Arrange
        var (harness, _) = Computed();
        var cleared = harness.Qualifiers(1)[0];
        _ = harness.ClearSlot(1, cleared);

        // Act
        var result = harness.AddOverride(1, cleared);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Override.Exists");
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
        harness.Stage(1).Overrides.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "ClearSlot should return Exists for a duplicate Clear of the same bowler on the same Stage")]
    public void ClearSlot_ShouldReturnExists_WhenBowlerWasAlreadyCleared()
    {
        // Arrange
        var (harness, _) = Computed();
        var cleared = harness.Qualifiers(1)[0];
        _ = harness.ClearSlot(1, cleared);

        // Act
        var result = harness.ClearSlot(1, cleared);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Override.Exists");
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
        harness.Stage(1).Overrides.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "ClearSlot should return Exists for a bowler who was added on the Stage")]
    public void ClearSlot_ShouldReturnExists_WhenBowlerWasAdded()
    {
        // Arrange
        var (harness, extra) = Computed();
        _ = harness.AddOverride(1, extra);

        // Act
        var result = harness.ClearSlot(1, extra);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Override.Exists");
        harness.Stage(1).Overrides.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "ClearSlot should return BowlerNotInSlot for a bowler who holds no slot")]
    public void ClearSlot_ShouldReturnBowlerNotInSlot_WhenBowlerHoldsNoSlot()
    {
        // Arrange
        var (harness, extra) = Computed();

        // Act
        var result = harness.ClearSlot(1, extra);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Override.BowlerNotInSlot");
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        harness.Stage(1).Overrides.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ClearSlot should return BowlerNotInSlot for a bowler who qualified in another Stage")]
    public void ClearSlot_ShouldReturnBowlerNotInSlot_WhenBowlerQualifiedInAnotherStage()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Qualifying).Advanced();

        // Act
        var result = harness.ClearSlot(2, harness.Qualifiers(1)[0]);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Override.BowlerNotInSlot");
    }

    [Fact(DisplayName = "ClearSlot should return BowlerNotInSlot for a Backfill entry")]
    public void ClearSlot_ShouldReturnBowlerNotInSlot_WhenBowlerIsABackfillEntry()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying);
        var replacement = BowlerId.New();
        harness.SetMembers(1, [.. harness.Qualifiers(1).Skip(1), replacement])
            .SetBackfill(Ready(harness, 1, BowlerPointsFactFactory.Create(replacement, 50)))
            .Advanced();

        // Act
        var result = harness.ClearSlot(1, replacement);

        // Assert
        harness.Series.FieldEntries.Single(e => e.IsBackfill).BowlerId.ShouldBe(replacement);
        result.FirstError.Code.ShouldBe("Series.Override.BowlerNotInSlot");
    }

    [Theory(DisplayName = "AddOverride and ClearSlot should return ReasonRequired for a blank reason and change nothing")]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    public void Override_ShouldReturnReasonRequired_WhenReasonIsBlank(string reason, bool clear)
    {
        // Arrange
        var (harness, extra) = Computed();
        var version = harness.Series.Version;

        // Act
        var result = clear ? harness.ClearSlot(1, harness.Qualifiers(1)[0], reason) : harness.AddOverride(1, extra, reason);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Override.Reason.Required");
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        harness.Stage(1).Overrides.ShouldBeEmpty();
        harness.Series.Version.ShouldBe(version);
        harness.Series.FieldEntries.Count.ShouldBe(AdvancementHarness.SlotCount);
    }

    [Theory(DisplayName = "AddOverride and ClearSlot should return ReasonTooLong for a reason over 500 characters and change nothing")]
    [InlineData(false)]
    [InlineData(true)]
    public void Override_ShouldReturnReasonTooLong_WhenReasonIsOver500Characters(bool clear)
    {
        // Arrange
        var (harness, extra) = Computed();
        var reason = new string('r', 501);
        var version = harness.Series.Version;

        // Act
        var result = clear ? harness.ClearSlot(1, harness.Qualifiers(1)[0], reason) : harness.AddOverride(1, extra, reason);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Override.Reason.TooLong");
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        result.FirstError.Description.ShouldContain("500");
        harness.Stage(1).Overrides.ShouldBeEmpty();
        harness.Series.Version.ShouldBe(version);
    }

    [Fact(DisplayName = "AddOverride should accept a reason of exactly 500 characters once trimmed")]
    public void AddOverride_ShouldAcceptReason_WhenItIs500CharactersAfterTrimming()
    {
        // Arrange
        var (harness, extra) = Computed();
        var reason = $"  {new string('r', 500)}  ";

        // Act
        var result = harness.AddOverride(1, extra, reason);

        // Assert
        result.IsError.ShouldBeFalse();
        harness.Stage(1).Overrides.Single().Reason.Length.ShouldBe(500);
    }

    [Fact(DisplayName = "AddOverride, ClearSlot and RemoveOverride should return a version conflict when the version is stale")]
    public void Override_ShouldReturnVersionConflict_WhenVersionIsStale()
    {
        // Arrange
        var (harness, extra) = Computed();
        var overrideId = harness.AddOverride(1, extra).Value.OverrideId!.Value;
        var stale = harness.Series.Version - 1;

        // Act
        var add = harness.AddOverride(1, extra, version: stale);
        var clear = harness.ClearSlot(1, harness.Qualifiers(1)[0], version: stale);
        var remove = harness.RemoveOverride(1, overrideId, version: stale);

        // Assert
        add.FirstError.ShouldBe(SeriesAdvancementErrors.VersionConflict);
        clear.FirstError.ShouldBe(SeriesAdvancementErrors.VersionConflict);
        remove.FirstError.ShouldBe(SeriesAdvancementErrors.VersionConflict);
        harness.Stage(1).Overrides.Count.ShouldBe(1);
        harness.Series.FieldEntries.ShouldContain(e => e.BowlerId == extra);
    }

    [Fact(DisplayName = "RemoveOverride should succeed before the version check when the override is already gone")]
    public void RemoveOverride_ShouldSucceed_WhenOverrideIsMissingEvenWithAStaleVersion()
    {
        // Arrange
        var (harness, _) = Computed();
        var version = harness.Series.Version;

        // Act
        var result = harness.RemoveOverride(1, StageOverrideId.New(), version: version + 99);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(AdvanceResult.None);
        harness.Series.Version.ShouldBe(version);
    }

    [Fact(DisplayName = "RemoveOverride should return NotFound for a Stage the Series does not have")]
    public void RemoveOverride_ShouldReturnNotFound_WhenStageIsUnknown()
    {
        // Arrange
        var (harness, _) = Computed();

        // Act
        var result = harness.Series.RemoveOverride(
            SeriesStageId.New(), StageOverrideId.New(), harness.Series.Version, harness.Facts, Now);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Advancement.StageNotFound");
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact(DisplayName = "RemoveOverride should return StageNotComputed when the Stage became Official")]
    public void RemoveOverride_ShouldReturnStageNotComputed_WhenStageIsOfficial()
    {
        // Arrange
        var (harness, extra) = Computed();
        var overrideId = harness.AddOverride(1, extra).Value.OverrideId!.Value;
        _ = harness.MakeDueOfficial(Now.AddDays(5));

        // Act
        var result = harness.RemoveOverride(1, overrideId);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Override.StageNotComputed");
        harness.Stage(1).Overrides.Count.ShouldBe(1);
    }
}