using ErrorOr;

using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Series;
using Neba.TestFactory.Storage;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class SeriesTests
{
    private static readonly DateOnly FinaleDate = new(2025, 9, 27);

    private static SeriesTestScenario ValidScenario()
        => new SeriesTestScenario()
            .Qualifying(1, 24, 8, endDay: 25)
            .Qualifying(3, 22)
            .Survivor(7, 12, 40)
            .Survivor(8, 23, 20)
            .Placement(9, 13);

    private static SeriesAggregate CreateValid(SeriesTestScenario scenario, SeriesFinaleDefinition? finale = null)
        => scenario.Create(finale).Value;

    private static SeriesAggregate Locked(SeriesTestScenario scenario, SeriesFinaleDefinition? finale = null)
    {
        var series = CreateValid(scenario, finale);
        series.Stages.First().Status = StageStatus.Computed;
        return series;
    }

    // ---- Create ----

    [Fact(DisplayName = "Create should succeed with a valid configuration")]
    public void Create_ShouldSucceed_WhenConfigurationIsValid()
    {
        // Arrange
        var scenario = ValidScenario();

        // Act
        var result = scenario.Create(SeriesFinaleDefinitionFactory.Create());

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Stages.Count.ShouldBe(5);
        result.Value.Stages.Select(s => s.Order).ShouldBe([1, 2, 3, 4, 5]);
        result.Value.Name.ShouldBe("Series");
    }

    [Fact(DisplayName = "Create should succeed with no Stages (an incomplete Series)")]
    public void Create_ShouldSucceed_WhenThereAreNoStages()
    {
        // Arrange
        var scenario = new SeriesTestScenario();

        // Act
        var result = scenario.Create();

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Stages.ShouldBeEmpty();
        result.Value.HasFinale.ShouldBeFalse();
    }

    [Theory(DisplayName = "Create should return NameRequired for a blank name")]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldReturnNameRequired_WhenNameIsBlank(string name)
    {
        // Arrange
        var seasonId = SeasonId.New();

        // Act
        var result = SeriesAggregate.Create(name, seasonId, null, [], null, new Dictionary<TournamentId, StageTournamentFacts>());

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.Name.Required");
    }

    [Fact(DisplayName = "Create should return NameTooLong when the name exceeds 127 characters")]
    public void Create_ShouldReturnNameTooLong_WhenNameIsTooLong()
    {
        // Arrange
        var name = new string('x', 128);

        // Act
        var result = SeriesAggregate.Create(name, SeasonId.New(), null, [], null, new Dictionary<TournamentId, StageTournamentFacts>());

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.Name.TooLong");
    }

    [Fact(DisplayName = "Create should trim the name")]
    public void Create_ShouldTrimName()
    {
        // Arrange & Act
        var series = SeriesFactory.Create(name: "  Padded  ");

        // Assert
        series.Name.ShouldBe("Padded");
    }

    [Fact(DisplayName = "Create should keep the logo it is given")]
    public void Create_ShouldKeepLogo()
    {
        // Arrange
        var logo = StoredFileFactory.Create();

        // Act
        var series = SeriesFactory.Create(logo: logo);

        // Assert
        series.Logo.ShouldBe(logo);
    }

    // ---- #1 ----

    [Fact(DisplayName = "Create should return TournamentNotFound when the tournament facts are missing (#1)")]
    public void Create_ShouldReturnTournamentNotFound_WhenTournamentIsMissing()
    {
        // Arrange
        var scenario = ValidScenario();
        scenario.Tournaments.Remove(scenario.Stages[1].TournamentId);

        // Act
        var result = scenario.Create();

        // Assert
        result.IsError.ShouldBeTrue();
        var error = result.Errors.Single();
        error.Code.ShouldBe("Series.Stage.TournamentNotFound");
        error.Metadata![SeriesErrors.StageOrderKey].ShouldBe(2);
    }

    [Fact(DisplayName = "Create should return DuplicateTournament when a tournament appears twice (#1)")]
    public void Create_ShouldReturnDuplicateTournament_WhenTournamentRepeats()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Qualifying(1, 24);
        scenario.Stages.Add(scenario.Stages[0]);

        // Act
        var result = scenario.Create();

        // Assert
        result.Errors.ShouldContain(e => e.Code == "Series.Stage.DuplicateTournament");
    }

    [Fact(DisplayName = "Create should return a Conflict when the tournament is in another Series (#1)")]
    public void Create_ShouldReturnConflict_WhenTournamentIsInAnotherSeries()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Add(StageKind.Qualifying, 8, new DateOnly(2025, 1, 24), otherSeries: "Other");

        // Act
        var result = scenario.Create();

        // Assert
        var error = result.Errors.Single();
        error.Code.ShouldBe("Series.Stage.TournamentInAnotherSeries");
        error.Type.ShouldBe(ErrorType.Conflict);
        error.Description.ShouldContain("Other");
    }

    // ---- #2 ----

    [Fact(DisplayName = "Create should return TournamentNotInSeason when the tournament is in a different season (#2)")]
    public void Create_ShouldReturnTournamentNotInSeason_WhenSeasonDiffers()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Add(StageKind.Qualifying, 8, new DateOnly(2025, 1, 24), season: SeasonId.New());

        // Act
        var result = scenario.Create();

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Stage.TournamentNotInSeason");
    }

    // ---- #3 ----

    [Fact(DisplayName = "Create should return KindOutOfOrder when Survivor comes before Qualifying (#3)")]
    public void Create_ShouldReturnKindOutOfOrder_WhenSurvivorBeforeQualifying()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Survivor(2, 1, 10).Qualifying(3, 1);

        // Act
        var result = scenario.Create();

        // Assert
        result.Errors.ShouldContain(e => e.Code == "Series.Stage.KindOutOfOrder");
    }

    [Fact(DisplayName = "Create should return KindOutOfOrder when Qualifying comes after Placement (#3)")]
    public void Create_ShouldReturnKindOutOfOrder_WhenQualifyingAfterPlacement()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Placement(2, 1).Qualifying(3, 1);

        // Act
        var result = scenario.Create();

        // Assert
        result.Errors.ShouldContain(e => e.Code == "Series.Stage.KindOutOfOrder");
    }

    [Fact(DisplayName = "Create should return MultiplePlacement for two Placement Stages (#3)")]
    public void Create_ShouldReturnMultiplePlacement_WhenTwoPlacementStages()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Placement(2, 1).Placement(3, 1);

        // Act
        var result = scenario.Create();

        // Assert
        result.Errors.ShouldContain(e => e.Code == "Series.Stage.MultiplePlacement");
    }

    [Fact(DisplayName = "Create should return PlacementNotLast when Placement is not the last Stage (#3)")]
    public void Create_ShouldReturnPlacementNotLast_WhenPlacementIsNotLast()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Qualifying(1, 1).Placement(2, 1).Survivor(3, 1, 10);

        // Act
        var result = scenario.Create();

        // Assert
        result.Errors.ShouldContain(e => e.Code == "Series.Stage.PlacementNotLast");
    }

    // ---- #4 ----

    [Fact(DisplayName = "Create should return DateNotAfterPrevious for an equal start date (#4)")]
    public void Create_ShouldReturnDateNotAfterPrevious_WhenStartDatesAreEqual()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Qualifying(1, 10).Qualifying(1, 10);

        // Act
        var result = scenario.Create();

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Stage.DateNotAfterPrevious");
    }

    [Fact(DisplayName = "Create should return DateNotAfterPrevious for an earlier start date (#4)")]
    public void Create_ShouldReturnDateNotAfterPrevious_WhenStartDateIsEarlier()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Qualifying(2, 10).Qualifying(1, 10);

        // Act
        var result = scenario.Create();

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Stage.DateNotAfterPrevious");
    }

    // ---- #5 ----

    [Theory(DisplayName = "Create should return SurvivorCountNotDecreasing when a Survivor count is not smaller (#5)")]
    [InlineData(40)]
    [InlineData(50)]
    public void Create_ShouldReturnSurvivorCountNotDecreasing_WhenCountIsNotSmaller(int second)
    {
        // Arrange
        var scenario = new SeriesTestScenario().Survivor(7, 1, 40).Survivor(8, 1, second);

        // Act
        var result = scenario.Create();

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Stage.SurvivorCountNotDecreasing");
    }

    // ---- #8 ----

    [Theory(DisplayName = "Create should return AdvancingCountRequired when a Qualifying or Survivor count is empty (#8)")]
    [InlineData("Qualifying")]
    [InlineData("Survivor")]
    public void Create_ShouldReturnAdvancingCountRequired_WhenCountIsEmpty(string kindName)
    {
        // Arrange
        var kind = StageKind.FromName(kindName);
        var scenario = new SeriesTestScenario().Add(
            kind, null, new DateOnly(2025, 1, 1), squadDate: kind == StageKind.Survivor ? new DateOnly(2025, 1, 1) : null, squadStart: kind == StageKind.Survivor ? new TimeOnly(9, 0) : null);

        // Act
        var result = scenario.Create();

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Stage.AdvancingCountRequired");
    }

    [Fact(DisplayName = "Create should return AdvancingCountNotAllowed when a Placement Stage has a count (#8)")]
    public void Create_ShouldReturnAdvancingCountNotAllowed_WhenPlacementHasCount()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Add(StageKind.Placement, 4, new DateOnly(2025, 9, 1));

        // Act
        var result = scenario.Create();

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Stage.AdvancingCountNotAllowed");
    }

    // ---- #6 ----

    [Theory(DisplayName = "Create should return FinaleCountNotPositive for a Finale count of zero or less (#6)")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldReturnFinaleCountNotPositive_WhenCountIsNotPositive(int count)
    {
        // Arrange
        var scenario = ValidScenario();

        // Act
        var result = scenario.Create(SeriesFinaleDefinitionFactory.Create(finalistCount: count));

        // Assert
        var error = result.Errors.Single();
        error.Code.ShouldBe("Series.Finale.FinalistCount.NotPositive");
        error.Metadata![SeriesErrors.PropertyKey].ShouldBe("Series.Finale.FinalistCount");
    }

    [Fact(DisplayName = "Create should return FinaleCountExceedsSurvivor when the Finale count is above the last Survivor count (#6)")]
    public void Create_ShouldReturnFinaleCountExceedsSurvivor_WhenCountIsTooLarge()
    {
        // Arrange
        var scenario = ValidScenario();

        // Act
        var result = scenario.Create(SeriesFinaleDefinitionFactory.Create(finalistCount: 21));

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Finale.FinalistCount.ExceedsSurvivor");
    }

    [Fact(DisplayName = "Create should allow a Finale count equal to the last Survivor count (#6)")]
    public void Create_ShouldSucceed_WhenFinaleCountEqualsLastSurvivorCount()
    {
        // Arrange
        var scenario = ValidScenario();

        // Act
        var result = scenario.Create(SeriesFinaleDefinitionFactory.Create(finalistCount: 20));

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Create should allow any positive Finale count when there is no Survivor Stage (#6)")]
    public void Create_ShouldSucceed_WhenNoSurvivorStage()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Qualifying(1, 10).Qualifying(2, 10);

        // Act
        var result = scenario.Create(SeriesFinaleDefinitionFactory.Create(finalistCount: 500));

        // Assert
        result.IsError.ShouldBeFalse();
    }

    // ---- #9 ----

    [Fact(DisplayName = "HasFinale should be true when a Finale is given and false when it is not (#9)")]
    public void HasFinale_ShouldFollowFinale()
    {
        // Arrange & Act
        var withFinale = CreateValid(ValidScenario(), SeriesFinaleDefinitionFactory.Create());
        var withoutFinale = CreateValid(ValidScenario());

        // Assert
        withFinale.HasFinale.ShouldBeTrue();
        withFinale.Finale!.FinalistCount.ShouldBe(4);
        withoutFinale.HasFinale.ShouldBeFalse();
        withoutFinale.Finale.ShouldBeNull();
    }

    [Fact(DisplayName = "Create should allow a Finale on a Series with no Placement Stage yet (#9)")]
    public void Create_ShouldSucceed_WhenFinaleHasNoPlacementStage()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Qualifying(1, 10).Survivor(2, 10, 20);

        // Act
        var result = scenario.Create(SeriesFinaleDefinitionFactory.Create(finalistCount: 4));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.HasFinale.ShouldBeTrue();
    }

    // ---- #11 ----

    [Fact(DisplayName = "Create should return StageNotBeforeFinale when the Finale is before the latest Stage (#11)")]
    public void Create_ShouldReturnStageNotBeforeFinale_WhenFinaleIsBefore()
    {
        // Arrange
        var scenario = ValidScenario();

        // Act
        var result = scenario.Create(SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 9, 1)));

        // Assert
        var error = result.Errors.Single();
        error.Code.ShouldBe("Series.Finale.Date.StageNotBefore");
        error.Metadata![SeriesErrors.PropertyKey].ShouldBe("Series.Finale.Date");
    }

    [Fact(DisplayName = "Create should return StageNotBeforeFinale when the Finale is the same day as the latest Stage's end (#11)")]
    public void Create_ShouldReturnStageNotBeforeFinale_WhenFinaleIsSameDay()
    {
        // Arrange
        var scenario = ValidScenario();

        // Act
        var result = scenario.Create(SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 9, 13)));

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Finale.Date.StageNotBefore");
    }

    [Fact(DisplayName = "Create should accept a Finale the day after the latest Stage's end (#11)")]
    public void Create_ShouldSucceed_WhenFinaleIsDayAfterLatestStage()
    {
        // Arrange
        var scenario = ValidScenario();

        // Act
        var result = scenario.Create(SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 9, 14)));

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Create should compare the Finale to a multi-day Stage's end date, not its start date (#11)")]
    public void Create_ShouldUseEndDate_WhenStageSpansDays()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Add(StageKind.Qualifying, 8, new DateOnly(2025, 9, 10), new DateOnly(2025, 9, 12));

        // Act
        var onEndDay = scenario.Create(SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 9, 12)));
        var dayAfter = scenario.Create(SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 9, 13)));

        // Assert
        onEndDay.IsError.ShouldBeTrue();
        dayAfter.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Create should name only the latest-ending Stage when the Finale precedes several (#11)")]
    public void Create_ShouldNameOnlyLatestStage_WhenFinaleIsBeforeSeveral()
    {
        // Arrange
        var scenario = new SeriesTestScenario()
            .Add(StageKind.Qualifying, 8, new DateOnly(2025, 5, 1), name: "Early")
            .Add(StageKind.Qualifying, 8, new DateOnly(2025, 6, 1), name: "Late");

        // Act
        var result = scenario.Create(SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 4, 1)));

        // Assert
        var error = result.Errors.Single();
        error.Description.ShouldContain("Late");
        error.Description.ShouldNotContain("Early");
    }

    [Fact(DisplayName = "Create should accept a Finale on a Series with no Stages (#11)")]
    public void Create_ShouldSucceed_WhenFinaleHasNoStages()
    {
        // Arrange
        var scenario = new SeriesTestScenario();

        // Act
        var result = scenario.Create(SeriesFinaleDefinitionFactory.Create());

        // Assert
        result.IsError.ShouldBeFalse();
    }

    // ---- aggregated errors ----

    [Fact(DisplayName = "Create should return every error in one pass, each with the right StageOrder")]
    public void Create_ShouldReturnAllErrors_InOnePass()
    {
        // Arrange
        var scenario = new SeriesTestScenario()
            .Add(StageKind.Qualifying, null, new DateOnly(2025, 1, 1))
            .Add(StageKind.Qualifying, 0, new DateOnly(2025, 2, 1))
            .Add(StageKind.Placement, 3, new DateOnly(2025, 3, 1));

        // Act
        var result = scenario.Create();

        // Assert
        var orders = result.Errors.ToDictionary(e => e.Code, e => (int)e.Metadata![SeriesErrors.StageOrderKey]);
        orders["Series.Stage.AdvancingCountRequired"].ShouldBe(1);
        orders["Series.Stage.AdvancingCountNotPositive"].ShouldBe(2);
        orders["Series.Stage.AdvancingCountNotAllowed"].ShouldBe(3);
    }

    // ---- ReplaceConfiguration ----

    [Fact(DisplayName = "ReplaceConfiguration should update a Stage in place and keep its id")]
    public void ReplaceConfiguration_ShouldKeepStageId_WhenUpdatingInPlace()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Qualifying(1, 10);
        var series = CreateValid(scenario);
        var stageId = series.Stages.Single().Id;
        var edited = new List<SeriesStageDefinition>
        {
            SeriesStageDefinitionFactory.Create(stageId: stageId, tournamentId: scenario.Stages[0].TournamentId, advancingCount: 5),
        };

        // Act
        var result = series.ReplaceConfiguration(edited, null, scenario.Tournaments);

        // Assert
        result.IsError.ShouldBeFalse();
        series.Stages.Single().Id.ShouldBe(stageId);
        series.Stages.Single().AdvancingCount.ShouldBe(5);
    }

    [Fact(DisplayName = "ReplaceConfiguration should refuse a reorder that breaks the date rule and change nothing")]
    public void ReplaceConfiguration_ShouldRefuseReorder_WhenDatesWouldBeOutOfOrder()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Qualifying(1, 10, 8).Qualifying(2, 10, 4);
        var series = CreateValid(scenario);
        var ids = series.Stages.Select(s => s.Id).ToList();
        var swapped = new List<SeriesStageDefinition>
        {
            SeriesStageDefinitionFactory.Create(stageId: ids[1], tournamentId: scenario.Stages[1].TournamentId, advancingCount: 4),
            SeriesStageDefinitionFactory.Create(stageId: ids[0], tournamentId: scenario.Stages[0].TournamentId, advancingCount: 8),
        };

        // Act
        var result = series.ReplaceConfiguration(swapped, null, scenario.Tournaments);

        // Assert
        result.IsError.ShouldBeTrue();
        series.Stages.Select(s => s.Id).ShouldBe(ids);
    }

    [Fact(DisplayName = "ReplaceConfiguration should remove a Stage that is missing from the submission")]
    public void ReplaceConfiguration_ShouldRemoveMissingStage()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Qualifying(1, 10).Qualifying(2, 10);
        var series = CreateValid(scenario);
        var keep = series.Stages.First();
        var submitted = new List<SeriesStageDefinition>
        {
            SeriesStageDefinitionFactory.Create(stageId: keep.Id, tournamentId: scenario.Stages[0].TournamentId),
        };

        // Act
        var result = series.ReplaceConfiguration(submitted, null, scenario.Tournaments);

        // Assert
        result.IsError.ShouldBeFalse();
        series.Stages.Single().Id.ShouldBe(keep.Id);
    }

    [Fact(DisplayName = "ReplaceConfiguration should add a new Stage when the submission has no id")]
    public void ReplaceConfiguration_ShouldAddNewStage()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Qualifying(1, 10);
        var series = CreateValid(scenario);
        scenario.Qualifying(2, 10);
        var submitted = new List<SeriesStageDefinition>
        {
            SeriesStageDefinitionFactory.Create(stageId: series.Stages.Single().Id, tournamentId: scenario.Stages[0].TournamentId),
            scenario.Stages[1],
        };

        // Act
        var result = series.ReplaceConfiguration(submitted, null, scenario.Tournaments);

        // Assert
        result.IsError.ShouldBeFalse();
        series.Stages.Count.ShouldBe(2);
        series.Stages.Select(s => s.Order).ShouldBe([1, 2]);
    }

    [Fact(DisplayName = "ReplaceConfiguration should return StageNotFound for an unknown Stage id")]
    public void ReplaceConfiguration_ShouldReturnStageNotFound_WhenIdIsUnknown()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Qualifying(1, 10);
        var series = CreateValid(scenario);
        var submitted = new List<SeriesStageDefinition>
        {
            SeriesStageDefinitionFactory.Create(stageId: SeriesStageId.New(), tournamentId: scenario.Stages[0].TournamentId),
        };

        // Act
        var result = series.ReplaceConfiguration(submitted, null, scenario.Tournaments);

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Stage.NotFound");
    }

    [Fact(DisplayName = "ReplaceConfiguration should add, change and remove the Finale")]
    public void ReplaceConfiguration_ShouldAddChangeAndRemoveFinale()
    {
        // Arrange
        var scenario = ValidScenario();
        var series = CreateValid(scenario);
        var center = CertificationNumberFactory.Create();

        // Act
        var added = series.ReplaceConfiguration(scenario.Stages, SeriesFinaleDefinitionFactory.Create(), scenario.Tournaments);
        var changed = series.ReplaceConfiguration(scenario.Stages, SeriesFinaleDefinitionFactory.Create(finalistCount: 6, bowlingCenterId: center), scenario.Tournaments);
        var changedFinale = series.Finale;
        var removed = series.ReplaceConfiguration(scenario.Stages, null, scenario.Tournaments);

        // Assert
        added.IsError.ShouldBeFalse();
        changed.IsError.ShouldBeFalse();
        changedFinale!.FinalistCount.ShouldBe(6);
        changedFinale.BowlingCenterId.ShouldBe(center);
        removed.IsError.ShouldBeFalse();
        series.Finale.ShouldBeNull();
    }

    [Fact(DisplayName = "ReplaceConfiguration should return Stage and Finale errors together and change nothing")]
    public void ReplaceConfiguration_ShouldReturnStageAndFinaleErrorsTogether()
    {
        // Arrange
        var scenario = ValidScenario();
        var series = CreateValid(scenario);
        var broken = new List<SeriesStageDefinition> { scenario.Stages[0] with { AdvancingCount = null } };

        // Act
        var result = series.ReplaceConfiguration(broken, SeriesFinaleDefinitionFactory.Create(finalistCount: 0), scenario.Tournaments);

        // Assert
        result.Errors.Select(e => e.Code).ShouldBe(
            ["Series.Stage.AdvancingCountRequired", "Series.Finale.FinalistCount.NotPositive"], ignoreOrder: true);
        series.Stages.Count.ShouldBe(5);
    }

    // ---- lock ----

    [Fact(DisplayName = "ReplaceConfiguration should return Locked when Stages change on a locked Series")]
    public void ReplaceConfiguration_ShouldReturnLocked_WhenStagesChange()
    {
        // Arrange
        var scenario = ValidScenario();
        var series = Locked(scenario);
        var changed = scenario.Stages.Take(4).ToList();

        // Act
        var result = series.ReplaceConfiguration(changed, null, scenario.Tournaments);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.Locked");
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
    }

    private static List<SeriesStageDefinition> Submitted(SeriesAggregate series, SeriesTestScenario scenario)
        => [.. series.Stages.Select((s, i) => scenario.Stages[i] with { StageId = s.Id })];

    [Fact(DisplayName = "ReplaceConfiguration should return Locked when a locked Series gains a Finale")]
    public void ReplaceConfiguration_ShouldReturnLocked_WhenFinaleIsAddedWhileLocked()
    {
        // Arrange
        var scenario = ValidScenario();
        var series = Locked(scenario);

        // Act
        var result = series.ReplaceConfiguration(Submitted(series, scenario), SeriesFinaleDefinitionFactory.Create(), scenario.Tournaments);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Locked");
        series.Finale.ShouldBeNull();
    }

    [Fact(DisplayName = "ReplaceConfiguration should return Locked when a locked Series loses its Finale")]
    public void ReplaceConfiguration_ShouldReturnLocked_WhenFinaleIsRemovedWhileLocked()
    {
        // Arrange
        var scenario = ValidScenario();
        var series = Locked(scenario, SeriesFinaleDefinitionFactory.Create());

        // Act
        var result = series.ReplaceConfiguration(Submitted(series, scenario), null, scenario.Tournaments);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Locked");
        series.Finale.ShouldNotBeNull();
    }

    [Fact(DisplayName = "ReplaceConfiguration should return Locked when a locked Series changes its Finale count")]
    public void ReplaceConfiguration_ShouldReturnLocked_WhenFinaleCountChangesWhileLocked()
    {
        // Arrange
        var scenario = ValidScenario();
        var series = Locked(scenario, SeriesFinaleDefinitionFactory.Create());

        // Act
        var result = series.ReplaceConfiguration(Submitted(series, scenario), SeriesFinaleDefinitionFactory.Create(finalistCount: 5), scenario.Tournaments);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Locked");
        series.Finale!.FinalistCount.ShouldBe(4);
    }

    [Fact(DisplayName = "ReplaceConfiguration should let a locked Series reschedule its Finale date, start time and center")]
    public void ReplaceConfiguration_ShouldUpdate_WhenOnlyFinaleScheduleChangesWhileLocked()
    {
        // Arrange
        var scenario = ValidScenario();
        var series = Locked(scenario, SeriesFinaleDefinitionFactory.Create());
        var center = CertificationNumberFactory.Create();
        var rescheduled = SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 10, 4), startTime: new TimeOnly(18, 30), bowlingCenterId: center);

        // Act
        var result = series.ReplaceConfiguration(Submitted(series, scenario), rescheduled, scenario.Tournaments);

        // Assert
        result.IsError.ShouldBeFalse();
        series.Finale!.Date.ShouldBe(new DateOnly(2025, 10, 4));
        series.Finale.StartTime.ShouldBe(new TimeOnly(18, 30));
        series.Finale.BowlingCenterId.ShouldBe(center);
    }

    [Fact(DisplayName = "ReplaceConfiguration should refuse a locked Series' Finale moved before its last Stage and change nothing (#11)")]
    public void ReplaceConfiguration_ShouldRefuse_WhenLockedFinaleMovesBeforeLastStage()
    {
        // Arrange
        var scenario = ValidScenario();
        var series = Locked(scenario, SeriesFinaleDefinitionFactory.Create());
        var early = SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 9, 1));

        // Act
        var result = series.ReplaceConfiguration(Submitted(series, scenario), early, scenario.Tournaments);

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Finale.Date.StageNotBefore");
        series.Finale!.Date.ShouldBe(FinaleDate);
    }

    [Fact(DisplayName = "ReplaceConfiguration should succeed when a locked Series is submitted unchanged")]
    public void ReplaceConfiguration_ShouldSucceed_WhenLockedAndUnchanged()
    {
        // Arrange
        var scenario = ValidScenario();
        var finale = SeriesFinaleDefinitionFactory.Create();
        var series = Locked(scenario, finale);

        // Act
        var result = series.ReplaceConfiguration(Submitted(series, scenario), finale, scenario.Tournaments);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Rename and ChangeLogo should work on a locked Series")]
    public void RenameAndChangeLogo_ShouldWork_WhenLocked()
    {
        // Arrange
        var series = Locked(ValidScenario());
        var logo = StoredFileFactory.Create();

        // Act
        var renamed = series.Rename("New name");
        series.ChangeLogo(logo);

        // Assert
        renamed.IsError.ShouldBeFalse();
        series.Name.ShouldBe("New name");
        series.Logo.ShouldBe(logo);
    }

    [Fact(DisplayName = "EnsureCanDelete should return Locked when the Series is locked and success when it is not")]
    public void EnsureCanDelete_ShouldFollowLock()
    {
        // Arrange
        var scenario = ValidScenario();
        var open = CreateValid(scenario);
        var locked = Locked(ValidScenario());

        // Act
        var openResult = open.EnsureCanDelete();
        var lockedResult = locked.EnsureCanDelete();

        // Assert
        openResult.IsError.ShouldBeFalse();
        lockedResult.FirstError.Code.ShouldBe("Series.Locked");
    }

    // ---- EF collection learning ----

    [Fact(DisplayName = "Stages should be read-only to callers while ReplaceConfiguration can still add to the backing list")]
    public void Stages_ShouldBeReadOnly_AndBackingListShouldAcceptAdds()
    {
        // Arrange
        var scenario = new SeriesTestScenario().Qualifying(1, 10);
        var series = SeriesFactory.Create(seasonId: scenario.SeasonId);

        // Act
        var result = series.ReplaceConfiguration(scenario.Stages, null, scenario.Tournaments);

        // Assert
        result.IsError.ShouldBeFalse();
        ((ICollection<SeriesStage>)series.Stages).IsReadOnly.ShouldBeTrue();
        series.Stages.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Create should accept the 2025 reference configuration with and without a Finale")]
    public void Create_ShouldSucceed_ForReferenceConfiguration()
    {
        // Arrange
        var withFinale = Series2025ReferenceFactory.Create(withFinale: true);
        var withoutFinale = Series2025ReferenceFactory.Create(withFinale: false);

        // Act
        var a = SeriesFactory.CreateFor(
            withFinale.Season,
            [.. withFinale.Tournaments.Select((t, i) => (t, withFinale.Stages[i].Kind, withFinale.Stages[i].AdvancingCount, withFinale.Stages[i].SquadDate, withFinale.Stages[i].SquadStartTime))],
            withFinale.Finale);
        var b = SeriesFactory.CreateFor(
            withoutFinale.Season,
            [.. withoutFinale.Tournaments.Select((t, i) => (t, withoutFinale.Stages[i].Kind, withoutFinale.Stages[i].AdvancingCount, withoutFinale.Stages[i].SquadDate, withoutFinale.Stages[i].SquadStartTime))],
            null);

        // Assert
        a.Stages.Count.ShouldBe(11);
        a.HasFinale.ShouldBeTrue();
        b.Stages.Count.ShouldBe(11);
        b.HasFinale.ShouldBeFalse();
        a.Stages.Last().AdvancingCount.ShouldBeNull();
    }
}