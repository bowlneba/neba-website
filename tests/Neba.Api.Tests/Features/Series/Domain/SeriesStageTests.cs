using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class SeriesStageTests
{
    private static readonly DateOnly Day = new(2025, 7, 12);
    private static readonly TimeOnly Nine = new(9, 0);

    private static StageTournamentFacts Facts(DateOnly? start = null, DateOnly? end = null)
        => StageTournamentFactsFactory.Create(startDate: start ?? Day, endDate: end ?? start ?? Day);

    private static SeriesStageDefinition Definition(StageKind kind, int? count, DateOnly? squadDate = null, TimeOnly? squadStart = null)
        => SeriesStageDefinitionFactory.Create(kind: kind, advancingCount: count, squadDate: squadDate, squadStartTime: squadStart);

    [Theory(DisplayName = "Create should return AdvancingCountNotPositive for a count of zero or less")]
    [InlineData("Qualifying", 0)]
    [InlineData("Qualifying", -1)]
    [InlineData("Survivor", 0)]
    [InlineData("Survivor", -1)]
    public void Create_ShouldReturnNotPositive_WhenCountIsZeroOrLess(string kind, int count)
    {
        // Arrange
        var stageKind = StageKind.FromName(kind);
        var definition = Definition(stageKind, count, stageKind == StageKind.Survivor ? Day : null, stageKind == StageKind.Survivor ? Nine : null);

        // Act
        var result = SeriesStage.Create(1, definition, Facts());

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Stage.AdvancingCountNotPositive");
    }

    [Theory(DisplayName = "Create should return AdvancingCountRequired for an empty Qualifying or Survivor count")]
    [InlineData("Qualifying")]
    [InlineData("Survivor")]
    public void Create_ShouldReturnRequired_WhenCountIsEmpty(string kind)
    {
        // Arrange
        var stageKind = StageKind.FromName(kind);
        var definition = Definition(stageKind, null, stageKind == StageKind.Survivor ? Day : null, stageKind == StageKind.Survivor ? Nine : null);

        // Act
        var result = SeriesStage.Create(1, definition, Facts());

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Stage.AdvancingCountRequired");
    }

    [Fact(DisplayName = "Create should accept an empty count on a Placement Stage")]
    public void Create_ShouldSucceed_WhenPlacementHasNoCount()
    {
        // Arrange
        var definition = Definition(StageKind.Placement, null);

        // Act
        var result = SeriesStage.Create(1, definition, Facts());

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.AdvancingCount.ShouldBeNull();
        result.Value.Status.ShouldBe(StageStatus.Pending);
    }

    [Fact(DisplayName = "Create should reject any count on a Placement Stage")]
    public void Create_ShouldReturnNotAllowed_WhenPlacementHasCount()
    {
        // Arrange
        var definition = Definition(StageKind.Placement, 4);

        // Act
        var result = SeriesStage.Create(1, definition, Facts());

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Stage.AdvancingCountNotAllowed");
    }

    [Fact(DisplayName = "Create should return SquadRequired for a Survivor Stage without a squad (#7)")]
    public void Create_ShouldReturnSquadRequired_WhenSurvivorHasNoSquad()
    {
        // Arrange
        var definition = Definition(StageKind.Survivor, 20);

        // Act
        var result = SeriesStage.Create(1, definition, Facts());

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Stage.SquadRequired");
    }

    [Theory(DisplayName = "Create should return SquadNotAllowed for a Qualifying or Placement Stage with a squad (#7)")]
    [InlineData("Qualifying", 8)]
    [InlineData("Placement", null)]
    public void Create_ShouldReturnSquadNotAllowed_WhenNonSurvivorHasSquad(string kind, int? count)
    {
        // Arrange
        var definition = Definition(StageKind.FromName(kind), count, Day, Nine);

        // Act
        var result = SeriesStage.Create(1, definition, Facts());

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Stage.SquadNotAllowed");
    }

    [Theory(DisplayName = "Create should return SquadDateOutsideTournament when the squad date is before or after the tournament (#7)")]
    [InlineData(11)]
    [InlineData(13)]
    public void Create_ShouldReturnOutside_WhenSquadDateIsOutsideTournament(int day)
    {
        // Arrange
        var definition = Definition(StageKind.Survivor, 20, new DateOnly(2025, 7, day), Nine);

        // Act
        var result = SeriesStage.Create(1, definition, Facts());

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Stage.SquadDateOutsideTournament");
    }

    [Fact(DisplayName = "Create should accept a squad on the second day of a multi-day tournament (#7)")]
    public void Create_ShouldSucceed_WhenSquadIsOnSecondDay()
    {
        // Arrange
        var definition = Definition(StageKind.Survivor, 20, Day.AddDays(1), Nine);

        // Act
        var result = SeriesStage.Create(1, definition, Facts(Day, Day.AddDays(1)));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.SquadDesignation!.Date.ShouldBe(Day.AddDays(1));
        result.Value.SquadDesignation.StartTime.ShouldBe(Nine);
        result.Value.SquadDesignation.SquadId.ShouldBeNull();
    }

    [Fact(DisplayName = "Matches should be true for the same values and false when any field differs")]
    public void Matches_ShouldCompareEveryField()
    {
        // Arrange
        var definition = Definition(StageKind.Survivor, 20, Day, Nine);
        var stage = SeriesStage.Create(3, definition, Facts()).Value;

        // Act & Assert
        stage.Matches(3, definition).ShouldBeTrue();
        stage.Matches(4, definition).ShouldBeFalse();
        stage.Matches(3, definition with { AdvancingCount = 19 }).ShouldBeFalse();
        stage.Matches(3, definition with { SquadDate = Day.AddDays(1) }).ShouldBeFalse();
        stage.Matches(3, definition with { SquadStartTime = new TimeOnly(10, 0) }).ShouldBeFalse();
        stage.Matches(3, definition with { TournamentId = Neba.Api.Features.Tournaments.Domain.TournamentId.New() }).ShouldBeFalse();
    }
}