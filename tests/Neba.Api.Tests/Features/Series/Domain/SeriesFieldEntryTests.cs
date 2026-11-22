using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class SeriesFieldEntryTests
{
    [Fact(DisplayName = "Create should return a Stage entry with a source Stage and a place")]
    public void Create_ShouldReturnStageEntry_WhenSourceAndPlaceAreGiven()
    {
        // Arrange
        var bowlerId = BowlerId.New();
        var stageId = SeriesStageId.New();

        // Act
        var result = SeriesFieldEntry.Create(bowlerId, stageId, 3, null, false);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BowlerId.ShouldBe(bowlerId);
        result.Value.SourceStageId.ShouldBe(stageId);
        result.Value.Place.ShouldBe(3);
        result.Value.BackfillPoints.ShouldBeNull();
        result.Value.ViaOverride.ShouldBeFalse();
        result.Value.IsBackfill.ShouldBeFalse();
    }

    [Fact(DisplayName = "Create should return a Stage entry added by an override")]
    public void Create_ShouldReturnOverrideEntry_WhenViaOverride()
    {
        // Arrange & Act
        var result = SeriesFieldEntry.Create(BowlerId.New(), SeriesStageId.New(), 9, null, true);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ViaOverride.ShouldBeTrue();
    }

    [Fact(DisplayName = "Create should return a Backfill entry with points, no source Stage and no place")]
    public void Create_ShouldReturnBackfillEntry_WhenPointsAreGiven()
    {
        // Arrange & Act
        var result = SeriesFieldEntry.Create(BowlerId.New(), null, null, 120, false);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.IsBackfill.ShouldBeTrue();
        result.Value.BackfillPoints.ShouldBe(120);
        result.Value.Place.ShouldBeNull();
        result.Value.SourceStageId.ShouldBeNull();
    }

    [Fact(DisplayName = "Create should give each entry its own ID")]
    public void Create_ShouldGiveEachEntryItsOwnId()
    {
        // Arrange & Act
        var first = SeriesFieldEntryFactory.Create();
        var second = SeriesFieldEntryFactory.Create();

        // Assert
        first.Id.ShouldNotBe(second.Id);
    }

    [Theory(DisplayName = "Create should return EntryInvalid for a Stage entry without a positive place or with points")]
    [InlineData(null, null)]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    [InlineData(1, 50)]
    public void Create_ShouldReturnEntryInvalid_WhenStageEntryIsMalformed(int? place, int? points)
    {
        // Arrange & Act
        var result = SeriesFieldEntry.Create(BowlerId.New(), SeriesStageId.New(), place, points, false);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.FieldEntry.Invalid");
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
    }

    [Theory(DisplayName = "Create should return EntryInvalid for a Backfill entry without positive points, with a place, or via an override")]
    [InlineData(null, null, false)]
    [InlineData(0, null, false)]
    [InlineData(-5, null, false)]
    [InlineData(100, 1, false)]
    [InlineData(100, null, true)]
    public void Create_ShouldReturnEntryInvalid_WhenBackfillEntryIsMalformed(int? points, int? place, bool viaOverride)
    {
        // Arrange & Act
        var result = SeriesFieldEntry.Create(BowlerId.New(), null, place, points, viaOverride);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.FieldEntry.Invalid");
    }

    [Fact(DisplayName = "Update should change the place and keep the source Stage")]
    public void Update_ShouldChangePlace_AndKeepSourceStage()
    {
        // Arrange
        var stageId = SeriesStageId.New();
        var entry = SeriesFieldEntryFactory.Create(sourceStageId: stageId, place: 1);

        // Act
        var result = entry.Update(4, null, false);

        // Assert
        result.IsError.ShouldBeFalse();
        entry.Place.ShouldBe(4);
        entry.SourceStageId.ShouldBe(stageId);
    }

    [Fact(DisplayName = "Update should return EntryInvalid and change nothing when the new values break the invariant")]
    public void Update_ShouldReturnEntryInvalid_AndChangeNothing()
    {
        // Arrange
        var entry = SeriesFieldEntryFactory.Create(sourceStageId: SeriesStageId.New(), place: 2);

        // Act
        var result = entry.Update(0, null, true);

        // Assert
        result.FirstError.Code.ShouldBe("Series.FieldEntry.Invalid");
        entry.Place.ShouldBe(2);
        entry.ViaOverride.ShouldBeFalse();
    }
}