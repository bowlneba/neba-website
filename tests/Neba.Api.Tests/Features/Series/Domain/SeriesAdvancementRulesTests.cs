using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class SeriesAdvancementRulesTests
{
    [Fact(DisplayName = "MakeOfficialBlocker should return null for a Computed, fresh Stage with nothing earlier open and no Backfill wait")]
    public void MakeOfficialBlocker_ShouldReturnNull_WhenStageCanBeMadeOfficial()
    {
        // Arrange

        // Act
        var blocker = SeriesAdvancementRules.MakeOfficialBlocker(
            StageStatus.Computed, isStale: false, awaitingBackfill: false, firstUnofficialEarlierOrder: null);

        // Assert
        blocker.ShouldBeNull();
    }

    [Theory(DisplayName = "MakeOfficialBlocker should return StageNotComputed for any status other than Computed")]
    [InlineData("Pending", TestDisplayName = "a Pending Stage")]
    [InlineData("Official", TestDisplayName = "an Official Stage")]
    public void MakeOfficialBlocker_ShouldReturnNotComputed_WhenStatusIsNotComputed(string statusName)
    {
        // Arrange
        var status = StageStatus.FromName(statusName);

        // Act
        var blocker = SeriesAdvancementRules.MakeOfficialBlocker(
            status, isStale: false, awaitingBackfill: false, firstUnofficialEarlierOrder: null);

        // Assert
        blocker.ShouldBe(SeriesAdvancementErrors.OfficialStageNotComputed);
    }

    [Fact(DisplayName = "MakeOfficialBlocker should check the status before staleness, earlier Stages and the Backfill")]
    public void MakeOfficialBlocker_ShouldReportStatusFirst_WhenSeveralBlockersApply()
    {
        // Arrange

        // Act
        var blocker = SeriesAdvancementRules.MakeOfficialBlocker(
            StageStatus.Pending, isStale: true, awaitingBackfill: true, firstUnofficialEarlierOrder: 1);

        // Assert
        blocker.ShouldBe(SeriesAdvancementErrors.OfficialStageNotComputed);
    }

    [Fact(DisplayName = "MakeOfficialBlocker should return StageStale before the earlier-Stage and Backfill checks")]
    public void MakeOfficialBlocker_ShouldReportStale_BeforeEarlierStageAndBackfill()
    {
        // Arrange

        // Act
        var blocker = SeriesAdvancementRules.MakeOfficialBlocker(
            StageStatus.Computed, isStale: true, awaitingBackfill: true, firstUnofficialEarlierOrder: 1);

        // Assert
        blocker.ShouldBe(SeriesAdvancementErrors.OfficialStageStale);
    }

    [Fact(DisplayName = "MakeOfficialBlocker should return EarlierStageNotOfficial with the order, before the Backfill check")]
    public void MakeOfficialBlocker_ShouldReportEarlierStage_BeforeBackfill()
    {
        // Arrange

        // Act
        var blocker = SeriesAdvancementRules.MakeOfficialBlocker(
            StageStatus.Computed, isStale: false, awaitingBackfill: true, firstUnofficialEarlierOrder: 2);

        // Assert
        blocker.ShouldNotBeNull();
        blocker.Value.Code.ShouldBe(SeriesAdvancementErrors.OfficialEarlierStageNotOfficial(2).Code);
        blocker.Value.Description.ShouldBe(SeriesAdvancementErrors.OfficialEarlierStageNotOfficial(2).Description);
    }

    [Fact(DisplayName = "MakeOfficialBlocker should return AwaitingBackfill when only the Backfill is outstanding")]
    public void MakeOfficialBlocker_ShouldReportAwaitingBackfill_WhenOnlyBackfillIsOutstanding()
    {
        // Arrange

        // Act
        var blocker = SeriesAdvancementRules.MakeOfficialBlocker(
            StageStatus.Computed, isStale: false, awaitingBackfill: true, firstUnofficialEarlierOrder: null);

        // Assert
        blocker.ShouldBe(SeriesAdvancementErrors.OfficialAwaitingBackfill);
    }

    [Fact(DisplayName = "FirstUnofficialEarlierOrder should return the lowest earlier order that is not Official")]
    public void FirstUnofficialEarlierOrder_ShouldReturnLowestOpenEarlierOrder()
    {
        // Arrange
        (int Order, StageStatus Status)[] stages =
        [
            (3, StageStatus.Pending),
            (1, StageStatus.Official),
            (2, StageStatus.Computed),
            (4, StageStatus.Pending),
        ];

        // Act
        var order = SeriesAdvancementRules.FirstUnofficialEarlierOrder(stages, order: 4);

        // Assert
        order.ShouldBe(2);
    }

    [Fact(DisplayName = "FirstUnofficialEarlierOrder should return null when every earlier Stage is Official")]
    public void FirstUnofficialEarlierOrder_ShouldReturnNull_WhenEarlierStagesAreOfficial()
    {
        // Arrange
        (int Order, StageStatus Status)[] stages =
        [
            (1, StageStatus.Official),
            (2, StageStatus.Official),
            (3, StageStatus.Computed),
        ];

        // Act
        var order = SeriesAdvancementRules.FirstUnofficialEarlierOrder(stages, order: 3);

        // Assert
        order.ShouldBeNull();
    }

    [Fact(DisplayName = "FirstUnofficialEarlierOrder should return null for the first Stage, and ignore later Stages")]
    public void FirstUnofficialEarlierOrder_ShouldReturnNull_ForTheFirstStage()
    {
        // Arrange
        (int Order, StageStatus Status)[] stages =
        [
            (1, StageStatus.Computed),
            (2, StageStatus.Pending),
        ];

        // Act
        var order = SeriesAdvancementRules.FirstUnofficialEarlierOrder(stages, order: 1);

        // Assert
        order.ShouldBeNull();
    }
}