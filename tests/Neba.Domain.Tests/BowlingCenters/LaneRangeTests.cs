using Neba.Domain.BowlingCenters;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;

namespace Neba.Domain.Tests.BowlingCenters;

[UnitTest]
[Component("BowlingCenters")]
public sealed class LaneRangeTests
{
    #region Create — PinFallType

#nullable disable
    [Fact(DisplayName = "Create returns error when pin fall type is null")]
    public void Create_ShouldReturnError_WhenPinFallTypeIsNull()
    {
        var result = LaneRange.Create(1, 2, null);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("LaneRange.PinFallType.Required");
    }
#nullable enable

    #endregion

    #region Create — StartLane

    [Theory(DisplayName = "Create returns error when start lane is even")]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public void Create_ShouldReturnError_WhenStartLaneIsEven(int startLane)
    {
        var result = LaneRange.Create(startLane, 10, PinFallType.FreeFall);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("LaneRange.StartLane.MustBeOdd");
    }

    [Fact(DisplayName = "Create returns error when start lane is negative")]
    public void Create_ShouldReturnError_WhenStartLaneIsNegative()
    {
        var result = LaneRange.Create(-1, 10, PinFallType.FreeFall);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("LaneRange.StartLane.MustBeOdd");
    }

    #endregion

    #region Create — EndLane

    [Theory(DisplayName = "Create returns error when end lane is odd")]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(9)]
    public void Create_ShouldReturnError_WhenEndLaneIsOdd(int endLane)
    {
        var result = LaneRange.Create(1, endLane, PinFallType.FreeFall);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("LaneRange.EndLane.MustBeEven");
    }

    [Fact(DisplayName = "Create returns error when end lane is zero")]
    public void Create_ShouldReturnError_WhenEndLaneIsZero()
    {
        var result = LaneRange.Create(1, 0, PinFallType.FreeFall);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("LaneRange.EndLane.MustBeEven");
    }

    [Theory(DisplayName = "Create returns error when end lane does not exceed start lane")]
    [InlineData(3, 2)]
    [InlineData(5, 4)]
    [InlineData(7, 6)]
    public void Create_ShouldReturnError_WhenEndLaneDoesNotExceedStartLane(int startLane, int endLane)
    {
        var result = LaneRange.Create(startLane, endLane, PinFallType.FreeFall);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("LaneRange.EndLane.MustExceedStartLane");
    }

    #endregion

    #region Create — valid

    [Theory(DisplayName = "Create returns LaneRange when inputs are valid")]
    [InlineData(1, 2)]
    [InlineData(1, 10)]
    [InlineData(3, 8)]
    [InlineData(1, 56)]
    public void Create_ShouldReturnLaneRange_WhenInputsAreValid(int startLane, int endLane)
    {
        var result = LaneRange.Create(startLane, endLane, PinFallType.FreeFall);

        result.IsError.ShouldBeFalse();
        result.Value.StartLane.ShouldBe(startLane);
        result.Value.EndLane.ShouldBe(endLane);
        result.Value.PinFallType.ShouldBe(PinFallType.FreeFall);
    }

    [Fact(DisplayName = "Create preserves pin fall type")]
    public void Create_ShouldPreservePinFallType_WhenStringPin()
    {
        var result = LaneRange.Create(1, 10, PinFallType.StringPin);

        result.IsError.ShouldBeFalse();
        result.Value.PinFallType.ShouldBe(PinFallType.StringPin);
    }

    #endregion

    #region PairCount

    [Theory(DisplayName = "PairCount returns correct count for range")]
    [InlineData(1, 2, 1)]
    [InlineData(1, 4, 2)]
    [InlineData(1, 10, 5)]
    [InlineData(1, 22, 11)]
    [InlineData(3, 8, 3)]
    public void PairCount_ShouldReturnCorrectCount(int startLane, int endLane, int expectedPairCount)
    {
        var range = LaneRangeFactory.Create(startLane, endLane);

        range.PairCount.ShouldBe(expectedPairCount);
    }

    #endregion

    #region LanePairs

    [Fact(DisplayName = "LanePairs returns single pair for minimum range")]
    public void LanePairs_ShouldReturnOnePair_ForMinimumRange()
    {
        var range = LaneRangeFactory.Create(startLane: 1, endLane: 2);

        range.LanePairs().ShouldBe([(1, 2)]);
    }

    [Fact(DisplayName = "LanePairs returns correct pairs for multi-pair range")]
    public void LanePairs_ShouldReturnCorrectPairs_ForMultiPairRange()
    {
        var range = LaneRangeFactory.Create(startLane: 3, endLane: 8);

        range.LanePairs().ShouldBe([(3, 4), (5, 6), (7, 8)]);
    }

    [Fact(DisplayName = "LanePairs returns pairs starting from non-one start lane")]
    public void LanePairs_ShouldRespectStartLane_WhenRangeDoesNotStartAtOne()
    {
        var range = LaneRangeFactory.Create(startLane: 27, endLane: 30);

        range.LanePairs().ShouldBe([(27, 28), (29, 30)]);
    }

    #endregion

    #region Record equality

    [Fact(DisplayName = "Two LaneRanges with the same values are equal")]
    public void Equality_ShouldBeEqual_WhenValuesAreTheSame()
    {
        var a = LaneRangeFactory.Create(startLane: 1, endLane: 10, pinFallType: PinFallType.FreeFall);
        var b = LaneRangeFactory.Create(startLane: 1, endLane: 10, pinFallType: PinFallType.FreeFall);

        a.ShouldBe(b);
    }

    [Fact(DisplayName = "Two LaneRanges with different start lanes are not equal")]
    public void Equality_ShouldNotBeEqual_WhenStartLanesDiffer()
    {
        var a = LaneRangeFactory.Create(startLane: 1, endLane: 10);
        var b = LaneRangeFactory.Create(startLane: 3, endLane: 10);

        a.ShouldNotBe(b);
    }

    [Fact(DisplayName = "Two LaneRanges with different end lanes are not equal")]
    public void Equality_ShouldNotBeEqual_WhenEndLanesDiffer()
    {
        var a = LaneRangeFactory.Create(startLane: 1, endLane: 10);
        var b = LaneRangeFactory.Create(startLane: 1, endLane: 12);

        a.ShouldNotBe(b);
    }

    [Fact(DisplayName = "Two LaneRanges with different pin fall types are not equal")]
    public void Equality_ShouldNotBeEqual_WhenPinFallTypesDiffer()
    {
        var a = LaneRangeFactory.Create(pinFallType: PinFallType.FreeFall);
        var b = LaneRangeFactory.Create(pinFallType: PinFallType.StringPin);

        a.ShouldNotBe(b);
    }

    #endregion
}