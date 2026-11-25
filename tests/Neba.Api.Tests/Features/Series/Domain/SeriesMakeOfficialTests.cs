using ErrorOr;

using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class SeriesMakeOfficialTests
{
    private static readonly DateTimeOffset Now = AdvancementHarness.Now;

    [Fact(DisplayName = "MakeOfficial should mark a Computed Stage Official, stamp OfficialAt and bump Version")]
    public void MakeOfficial_ShouldMarkStageOfficial()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();
        var stage = harness.Stage(1);
        var version = harness.Series.Version;

        // Act
        var result = harness.Series.MakeOfficial(stage.Id, version, Now.AddHours(3));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeTrue();
        result.Value.StatusChanged.ShouldBeTrue();
        result.Value.OfficialStageIds.ShouldBe([stage.Id]);
        stage.Status.ShouldBe(StageStatus.Official);
        stage.OfficialAt.ShouldBe(Now.AddHours(3));
        harness.Series.Version.ShouldBe(version + 1);
    }

    [Fact(DisplayName = "MakeOfficial should leave an Official Stage alone, even with a stale Version")]
    public void MakeOfficial_ShouldDoNothing_WhenStageIsAlreadyOfficialAndVersionIsStale()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();
        var stage = harness.Stage(1);
        var staleVersion = harness.Series.Version;
        _ = harness.Series.MakeOfficial(stage.Id, staleVersion, Now);
        var version = harness.Series.Version;

        // Act
        var result = harness.Series.MakeOfficial(stage.Id, staleVersion, Now.AddHours(1));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeFalse();
        harness.Series.Version.ShouldBe(version);
    }

    [Fact(DisplayName = "MakeOfficial should return NotFound for an unknown Stage")]
    public void MakeOfficial_ShouldReturnNotFound_WhenStageIsUnknown()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();

        // Act
        var result = harness.Series.MakeOfficial(SeriesStageId.New(), harness.Series.Version, Now);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact(DisplayName = "MakeOfficial should return the version conflict for a stale Version")]
    public void MakeOfficial_ShouldReturnVersionConflict_WhenVersionIsStale()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();

        // Act
        var result = harness.Series.MakeOfficial(harness.Stage(1).Id, harness.Series.Version - 1, Now);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.VersionConflict);
        harness.Stage(1).Status.ShouldBe(StageStatus.Computed);
    }

    [Fact(DisplayName = "MakeOfficial should return a Conflict for a Pending Stage")]
    public void MakeOfficial_ShouldReturnConflict_WhenStageIsPending()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying);

        // Act
        var result = harness.Series.MakeOfficial(harness.Stage(1).Id, harness.Series.Version, Now);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.OfficialStageNotComputed);
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact(DisplayName = "MakeOfficial should make a computed Placement Stage Official once the earlier Stages are Official")]
    public void MakeOfficial_ShouldMakePlacementOfficial_WhenEarlierStagesAreOfficial()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Placement).Advanced();
        _ = harness.MakeReadyOfficial();

        // Act
        var result = harness.Series.MakeOfficial(harness.Stage(2).Id, harness.Series.Version, Now);

        // Assert
        result.IsError.ShouldBeFalse();
        harness.Stage(2).Status.ShouldBe(StageStatus.Official);
    }

    [Fact(DisplayName = "MakeOfficial should return a Conflict for a stale Stage")]
    public void MakeOfficial_ShouldReturnConflict_WhenStageIsStale()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Qualifying).Advanced();
        harness.SetResults(1, []);
        _ = harness.Advance(now: Now.AddHours(1));
        harness.Stage(1).IsStale.ShouldBeTrue();

        // Act
        var result = harness.Series.MakeOfficial(harness.Stage(1).Id, harness.Series.Version, Now);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.OfficialStageStale);
    }

    [Fact(DisplayName = "MakeOfficial should name the first earlier Stage that is not Official")]
    public void MakeOfficial_ShouldNameEarlierStage_WhenItIsNotOfficial()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Survivor, StageKind.Survivor).Advanced();

        // Act
        var result = harness.Series.MakeOfficial(harness.Stage(3).Id, harness.Series.Version, Now);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.OfficialEarlierStageNotOfficial(1));
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact(DisplayName = "MakeOfficial should return a Conflict for the last Qualifying Stage until the Backfill ran")]
    public void MakeOfficial_ShouldReturnConflict_WhenLastQualifyingStageAwaitsBackfill()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying);
        var members = harness.Qualifiers(1).Skip(1).ToList(); // the place 1 bowler is not a member, so a slot is vacant
        harness.SetMembers(1, members).Advanced();

        // Act
        var result = harness.Series.MakeOfficial(harness.Stage(1).Id, harness.Series.Version, Now);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.OfficialAwaitingBackfill);
        harness.Stage(1).Status.ShouldBe(StageStatus.Computed);
    }

    [Fact(DisplayName = "MakeOfficial should accept a later Stage once every earlier Stage is Official")]
    public void MakeOfficial_ShouldAcceptLaterStage_WhenEarlierStagesAreOfficial()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying, StageKind.Survivor).Advanced();
        harness.Series.MakeOfficial(harness.Stage(1).Id, harness.Series.Version, Now).IsError.ShouldBeFalse();

        // Act
        var result = harness.Series.MakeOfficial(harness.Stage(2).Id, harness.Series.Version, Now.AddHours(1));

        // Assert
        result.IsError.ShouldBeFalse();
        harness.Stage(2).Status.ShouldBe(StageStatus.Official);
    }

    [Fact(DisplayName = "MakeOfficial should leave an Official Stage alone")]
    public void MakeOfficial_ShouldDoNothing_WhenStageIsAlreadyOfficial()
    {
        // Arrange
        var harness = AdvancementHarness.Create(StageKind.Qualifying).Advanced();
        harness.Series.MakeOfficial(harness.Stage(1).Id, harness.Series.Version, Now).IsError.ShouldBeFalse();
        var version = harness.Series.Version;

        // Act
        var result = harness.Series.MakeOfficial(harness.Stage(1).Id, version, Now.AddDays(1));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Changed.ShouldBeFalse();
        harness.Stage(1).OfficialAt.ShouldBe(Now);
        harness.Series.Version.ShouldBe(version);
    }
}