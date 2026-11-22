using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class SeriesSquadTests
{
    private static readonly DateTimeOffset Now = AdvancementHarness.Now;

    private static AdvancementHarness SurvivorHarness()
        => AdvancementHarness.Create(StageKind.Qualifying, StageKind.Survivor);

    private static StageSquadFact SquadLike(AdvancementHarness harness, TimeOnly? startTime = null, DateOnly? date = null, bool reverseTotals = false)
    {
        var designated = harness.Tournament(2).Squads.Single();
        var totals = reverseTotals ? AdvancementHarness.Totals(harness.Field.Reverse()) : designated.Totals;

        return StageSquadFactFactory.Create(
            localDate: date ?? designated.LocalDate,
            localStartTime: startTime ?? designated.LocalStartTime,
            totals: totals);
    }

    private static List<(BowlerId BowlerId, int? BlockTotal, bool Advanced)> Outcomes(SeriesStage stage)
        => [.. stage.SurvivorOutcomes.OrderBy(o => o.BowlerId).Select(o => (o.BowlerId, o.BlockTotal, o.Advanced))];

    // ---- resolve ----

    [Fact(DisplayName = "Advance should resolve the designated squad by date and start time")]
    public void Advance_ShouldResolveSquad_WhenDateAndStartTimeMatch()
    {
        // Arrange
        var harness = SurvivorHarness();
        var stage = harness.Stage(2);
        var squadId = harness.Tournament(2).Squads.Single().SquadId;

        // Act
        _ = harness.Advance();

        // Assert
        stage.SquadDesignation!.IsResolved.ShouldBeTrue();
        stage.SquadDesignation.SquadId.ShouldBe(squadId);
        stage.Status.ShouldBe(StageStatus.Computed);
        harness.Series.HasStageResolvedTo(squadId).ShouldBeTrue();
        harness.Series.HasStageResolvedTo(SquadId.New()).ShouldBeFalse();
    }

    [Fact(DisplayName = "Advance should match the start time to the minute and ignore seconds")]
    public void Advance_ShouldMatchStartTimeToTheMinute()
    {
        // Arrange
        var harness = SurvivorHarness();
        var squad = SquadLike(harness, new TimeOnly(9, 0, 45));
        harness.SetSquads(2, [squad]);

        // Act
        _ = harness.Advance();

        // Assert
        harness.Stage(2).SquadDesignation!.SquadId.ShouldBe(squad.SquadId);
    }

    [Fact(DisplayName = "Advance should leave the Stage Pending when the only squad starts a minute later")]
    public void Advance_ShouldLeaveStagePending_WhenStartTimeDiffers()
    {
        // Arrange
        var harness = SurvivorHarness();
        harness.SetSquads(2, [SquadLike(harness, new TimeOnly(9, 1))]);

        // Act
        _ = harness.Advance();

        // Assert
        harness.Stage(2).SquadDesignation!.IsResolved.ShouldBeFalse();
        harness.Stage(2).Status.ShouldBe(StageStatus.Pending);
        harness.Stage(2).IsStale.ShouldBeFalse();
    }

    [Fact(DisplayName = "Advance should leave the Stage Pending when the only squad bowls on another date")]
    public void Advance_ShouldLeaveStagePending_WhenDateDiffers()
    {
        // Arrange
        var harness = SurvivorHarness();
        var designated = harness.Tournament(2).Squads.Single();
        harness.SetSquads(2, [SquadLike(harness, date: designated.LocalDate.AddDays(1))]);

        // Act
        _ = harness.Advance();

        // Assert
        harness.Stage(2).SquadDesignation!.IsResolved.ShouldBeFalse();
        harness.Stage(2).Status.ShouldBe(StageStatus.Pending);
    }

    [Fact(DisplayName = "Advance should leave the Stage Pending when two squads match the date and start time")]
    public void Advance_ShouldLeaveStagePending_WhenTwoSquadsMatch()
    {
        // Arrange
        var harness = SurvivorHarness();
        harness.SetSquads(2, [SquadLike(harness), SquadLike(harness)]);

        // Act
        _ = harness.Advance();

        // Assert
        harness.Stage(2).SquadDesignation!.IsResolved.ShouldBeFalse();
        harness.Stage(2).Status.ShouldBe(StageStatus.Pending);
        harness.Stage(2).IsStale.ShouldBeFalse();
    }

    [Fact(DisplayName = "Advance should leave a resolved Stage Pending when its squad has no score rows")]
    public void Advance_ShouldLeaveStagePending_WhenSquadHasNoScores()
    {
        // Arrange
        var harness = SurvivorHarness();
        harness.SetSquads(2, [StageSquadFactFactory.Create(localDate: harness.Tournament(2).Squads.Single().LocalDate)]);

        // Act
        _ = harness.Advance();

        // Assert
        harness.Stage(2).SquadDesignation!.IsResolved.ShouldBeTrue();
        harness.Stage(2).Status.ShouldBe(StageStatus.Pending);
    }

    // ---- vanished squad ----

    [Fact(DisplayName = "Advance should revert a vanished squad, mark the Stage Stale and keep the stored output")]
    public void Advance_ShouldRevertVanishedSquadAndMarkStale()
    {
        // Arrange
        var harness = SurvivorHarness().Advanced();
        var stage = harness.Stage(2);
        var storedOutcomes = Outcomes(stage);
        var version = harness.Series.Version;
        harness.SetSquads(2, []);

        // Act
        var result = harness.Advance(2, Now.AddDays(1));

        // Assert
        stage.SquadDesignation!.IsResolved.ShouldBeFalse();
        stage.Status.ShouldBe(StageStatus.Computed);
        stage.IsStale.ShouldBeTrue();
        Outcomes(stage).ShouldBe(storedOutcomes);
        result.Changed.ShouldBeTrue();
        result.StaleStageIds.ShouldBe([stage.Id]);
        harness.Series.Version.ShouldBe(version + 1);
    }

    [Fact(DisplayName = "Advance should resolve again to a new squad with the same date and start time and not mark the Stage Stale")]
    public void Advance_ShouldResolveToReplacementSquad_WhenSquadIdChanged()
    {
        // Arrange
        var harness = SurvivorHarness().Advanced();
        var stage = harness.Stage(2);
        var replacement = SquadLike(harness);
        harness.SetSquads(2, [replacement]);

        // Act
        var result = harness.Advance(2, Now.AddDays(1));

        // Assert
        stage.SquadDesignation!.SquadId.ShouldBe(replacement.SquadId);
        stage.IsStale.ShouldBeFalse();
        stage.Status.ShouldBe(StageStatus.Computed);
        result.Changed.ShouldBeTrue();
        result.StaleStageIds.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Advance should mark an Official Stage Stale when its squad vanishes and leave the output and status")]
    public void Advance_ShouldMarkOfficialStageStale_WhenSquadVanishes()
    {
        // Arrange
        var harness = SurvivorHarness().Advanced();
        _ = harness.MakeDueOfficial();
        var stage = harness.Stage(2);
        var storedOutcomes = Outcomes(stage);
        harness.SetSquads(2, []);

        // Act
        _ = harness.Advance(2, Now.AddDays(3));

        // Assert
        stage.Status.ShouldBe(StageStatus.Official);
        stage.IsStale.ShouldBeTrue();
        stage.SquadDesignation!.IsResolved.ShouldBeFalse();
        Outcomes(stage).ShouldBe(storedOutcomes);
    }

    // ---- PickSquad ----

    [Fact(DisplayName = "PickSquad should resolve one of two matching squads and compute the Stage")]
    public void PickSquad_ShouldResolveAmbiguousSquadAndCompute()
    {
        // Arrange
        var harness = SurvivorHarness();
        var first = SquadLike(harness);
        var second = SquadLike(harness, reverseTotals: true);
        harness.SetSquads(2, [first, second]).Advanced();

        // Act
        var result = harness.PickSquad(2, second.SquadId, now: Now.AddHours(1));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ComputedStageIds.ShouldBe([harness.Stage(2).Id]);
        harness.Stage(2).SquadDesignation!.SquadId.ShouldBe(second.SquadId);
        harness.Stage(2).Status.ShouldBe(StageStatus.Computed);
        harness.Stage(2).SurvivorOutcomes.Count.ShouldBe(harness.Field.Count);
    }

    [Fact(DisplayName = "PickSquad should recompute a Computed Stage when the new squad changes the output")]
    public void PickSquad_ShouldRecomputeComputedStage_WhenOutputChanges()
    {
        // Arrange
        var harness = SurvivorHarness();
        var designated = harness.Tournament(2).Squads.Single();
        var other = SquadLike(harness, new TimeOnly(18, 0), reverseTotals: true);
        harness.SetSquads(2, [designated, other]).Advanced();
        var before = Outcomes(harness.Stage(2));

        // Act
        var result = harness.PickSquad(2, other.SquadId, now: Now.AddHours(1));

        // Assert
        result.IsError.ShouldBeFalse();
        Outcomes(harness.Stage(2)).ShouldNotBe(before);
        harness.Stage(2).ComputedAt.ShouldBe(Now.AddHours(1));
    }

    [Fact(DisplayName = "PickSquad should mark an Official Stage Stale, not rewrite it, when the new squad changes the output")]
    public void PickSquad_ShouldMarkOfficialStageStale_WhenOutputChanges()
    {
        // Arrange
        var harness = SurvivorHarness();
        var designated = harness.Tournament(2).Squads.Single();
        var other = SquadLike(harness, new TimeOnly(18, 0), reverseTotals: true);
        harness.SetSquads(2, [designated, other]).Advanced();
        _ = harness.MakeDueOfficial();
        var before = Outcomes(harness.Stage(2));

        // Act
        var result = harness.PickSquad(2, other.SquadId, now: Now.AddDays(3));

        // Assert
        result.IsError.ShouldBeFalse();
        harness.Stage(2).Status.ShouldBe(StageStatus.Official);
        harness.Stage(2).IsStale.ShouldBeTrue();
        harness.Stage(2).SquadDesignation!.SquadId.ShouldBe(other.SquadId);
        Outcomes(harness.Stage(2)).ShouldBe(before);
    }

    [Fact(DisplayName = "PickSquad should return NotFound for a Stage the Series does not have")]
    public void PickSquad_ShouldReturnNotFound_WhenStageIsUnknown()
    {
        // Arrange
        var harness = SurvivorHarness().Advanced();

        // Act
        var result = harness.Series.PickSquad(SeriesStageId.New(), SquadId.New(), harness.Series.Version, harness.Facts, Now);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Advancement.StageNotFound");
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact(DisplayName = "PickSquad should return a version conflict when the version is stale")]
    public void PickSquad_ShouldReturnVersionConflict_WhenVersionIsStale()
    {
        // Arrange
        var harness = SurvivorHarness().Advanced();

        // Act
        var result = harness.PickSquad(2, SquadId.New(), version: harness.Series.Version - 1);

        // Assert
        result.FirstError.ShouldBe(SeriesAdvancementErrors.VersionConflict);
    }

    [Fact(DisplayName = "PickSquad should return StageNotSurvivor for a Qualifying Stage")]
    public void PickSquad_ShouldReturnStageNotSurvivor_WhenStageIsQualifying()
    {
        // Arrange
        var harness = SurvivorHarness().Advanced();

        // Act
        var result = harness.PickSquad(1, SquadId.New());

        // Assert
        result.FirstError.Code.ShouldBe("Series.Squad.StageNotSurvivor");
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact(DisplayName = "PickSquad should return SquadNotInTournament for a squad the tournament does not have")]
    public void PickSquad_ShouldReturnSquadNotInTournament_WhenSquadIsUnknown()
    {
        // Arrange
        var harness = SurvivorHarness().Advanced();
        var version = harness.Series.Version;

        // Act
        var result = harness.PickSquad(2, SquadId.New());

        // Assert
        result.FirstError.Code.ShouldBe("Series.Squad.NotInTournament");
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        harness.Series.Version.ShouldBe(version);
    }

    [Fact(DisplayName = "PickSquad should return SquadNotInTournament when the facts hold no tournament for the Stage")]
    public void PickSquad_ShouldReturnSquadNotInTournament_WhenTournamentIsMissing()
    {
        // Arrange
        var harness = SurvivorHarness().Advanced();
        var squadId = harness.Stage(2).SquadDesignation!.SquadId!.Value;

        // Act
        var result = harness.Series.PickSquad(harness.Stage(2).Id, squadId, harness.Series.Version, AdvancementFacts.Empty, Now);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Squad.NotInTournament");
    }
}