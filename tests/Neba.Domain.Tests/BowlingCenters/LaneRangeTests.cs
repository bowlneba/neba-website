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
        // Act
        var result = LaneRange.Create(1, 2, null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("LaneRange.PinFallType.Required");
    }
#nullable enable

    #endregion

    #region Create — StartLane

    [Theory(DisplayName = "Create returns error when start lane is even")]
    [InlineData(0, TestDisplayName = "Lane 0 is even and invalid")]
    [InlineData(2, TestDisplayName = "Lane 2 is even and invalid")]
    [InlineData(4, TestDisplayName = "Lane 4 is even and invalid")]
    public void Create_ShouldReturnError_WhenStartLaneIsEven(int startLane)
    {
        // Act
        var result = LaneRange.Create(startLane, 10, PinFallType.FreeFall);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("LaneRange.StartLane.MustBeOdd");
    }

    [Fact(DisplayName = "Create returns error when start lane is negative")]
    public void Create_ShouldReturnError_WhenStartLaneIsNegative()
    {
        // Act
        var result = LaneRange.Create(-1, 10, PinFallType.FreeFall);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("LaneRange.StartLane.MustBeOdd");
    }

    #endregion

    #region Create — EndLane

    [Theory(DisplayName = "Create returns error when end lane is odd")]
    [InlineData(1, TestDisplayName = "Lane 1 is odd and invalid for end lane")]
    [InlineData(3, TestDisplayName = "Lane 3 is odd and invalid for end lane")]
    [InlineData(9, TestDisplayName = "Lane 9 is odd and invalid for end lane")]
    public void Create_ShouldReturnError_WhenEndLaneIsOdd(int endLane)
    {
        // Act
        var result = LaneRange.Create(1, endLane, PinFallType.FreeFall);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("LaneRange.EndLane.MustBeEven");
    }

    [Fact(DisplayName = "Create returns error when end lane is zero")]
    public void Create_ShouldReturnError_WhenEndLaneIsZero()
    {
        // Act
        var result = LaneRange.Create(1, 0, PinFallType.FreeFall);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("LaneRange.EndLane.MustBeEven");
    }

    [Theory(DisplayName = "Create returns error when end lane does not exceed start lane")]
    [InlineData(3, 2, TestDisplayName = "End lane 2 does not exceed start lane 3")]
    [InlineData(5, 4, TestDisplayName = "End lane 4 does not exceed start lane 5")]
    [InlineData(7, 6, TestDisplayName = "End lane 6 does not exceed start lane 7")]
    public void Create_ShouldReturnError_WhenEndLaneDoesNotExceedStartLane(int startLane, int endLane)
    {
        // Act
        var result = LaneRange.Create(startLane, endLane, PinFallType.FreeFall);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("LaneRange.EndLane.MustExceedStartLane");
    }

    #endregion

    #region Create — valid

    [Theory(DisplayName = "Create returns LaneRange when inputs are valid")]
    [InlineData(1, 2, TestDisplayName = "Minimum valid range of 1 pair")]
    [InlineData(1, 10, TestDisplayName = "Range of 5 pairs starting at lane 1")]
    [InlineData(3, 8, TestDisplayName = "Range of 3 pairs starting at lane 3")]
    [InlineData(1, 56, TestDisplayName = "Large range of 28 pairs")]
    public void Create_ShouldReturnLaneRange_WhenInputsAreValid(int startLane, int endLane)
    {
        // Act
        var result = LaneRange.Create(startLane, endLane, PinFallType.FreeFall);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.StartLane.ShouldBe(startLane);
        result.Value.EndLane.ShouldBe(endLane);
        result.Value.PinFallType.ShouldBe(PinFallType.FreeFall);
    }

    [Fact(DisplayName = "Create preserves pin fall type")]
    public void Create_ShouldPreservePinFallType_WhenStringPin()
    {
        // Act
        var result = LaneRange.Create(1, 10, PinFallType.StringPin);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.PinFallType.ShouldBe(PinFallType.StringPin);
    }

    #endregion

    #region PairCount

    [Theory(DisplayName = "PairCount returns correct count for range")]
    [InlineData(1, 2, 1, TestDisplayName = "Lanes 1–2 = 1 pair")]
    [InlineData(1, 4, 2, TestDisplayName = "Lanes 1–4 = 2 pairs")]
    [InlineData(1, 10, 5, TestDisplayName = "Lanes 1–10 = 5 pairs")]
    [InlineData(1, 22, 11, TestDisplayName = "Lanes 1–22 = 11 pairs")]
    [InlineData(3, 8, 3, TestDisplayName = "Lanes 3–8 = 3 pairs")]
    public void PairCount_ShouldReturnCorrectCount(int startLane, int endLane, int expectedPairCount)
    {
        // Arrange
        var range = LaneRangeFactory.Create(startLane, endLane);

        // Act
        var result = range.PairCount;

        // Assert
        result.ShouldBe(expectedPairCount);
    }

    #endregion

    #region LanePairs

    [Fact(DisplayName = "LanePairs returns single pair for minimum range")]
    public void LanePairs_ShouldReturnOnePair_ForMinimumRange()
    {
        // Arrange
        var range = LaneRangeFactory.Create(startLane: 1, endLane: 2);

        // Act
        var result = range.LanePairs();

        // Assert
        result.ShouldBe([(1, 2)]);
    }

    [Fact(DisplayName = "LanePairs returns correct pairs for multi-pair range")]
    public void LanePairs_ShouldReturnCorrectPairs_ForMultiPairRange()
    {
        // Arrange
        var range = LaneRangeFactory.Create(startLane: 3, endLane: 8);

        // Act
        var result = range.LanePairs();

        // Assert
        result.ShouldBe([(3, 4), (5, 6), (7, 8)]);
    }

    [Fact(DisplayName = "LanePairs returns pairs starting from non-one start lane")]
    public void LanePairs_ShouldRespectStartLane_WhenRangeDoesNotStartAtOne()
    {
        // Arrange
        var range = LaneRangeFactory.Create(startLane: 27, endLane: 30);

        // Act
        var result = range.LanePairs();

        // Assert
        result.ShouldBe([(27, 28), (29, 30)]);
    }

    #endregion

    #region Record equality

    [Fact(DisplayName = "Two LaneRanges with the same values are equal")]
    public void Equality_ShouldBeEqual_WhenValuesAreTheSame()
    {
        // Arrange
        var a = LaneRangeFactory.Create(startLane: 1, endLane: 10, pinFallType: PinFallType.FreeFall);
        var b = LaneRangeFactory.Create(startLane: 1, endLane: 10, pinFallType: PinFallType.FreeFall);

        // Act & Assert
        a.ShouldBe(b);
    }

    [Fact(DisplayName = "Two LaneRanges with different start lanes are not equal")]
    public void Equality_ShouldNotBeEqual_WhenStartLanesDiffer()
    {
        // Arrange
        var a = LaneRangeFactory.Create(startLane: 1, endLane: 10);
        var b = LaneRangeFactory.Create(startLane: 3, endLane: 10);

        // Act & Assert
        a.ShouldNotBe(b);
    }

    [Fact(DisplayName = "Two LaneRanges with different end lanes are not equal")]
    public void Equality_ShouldNotBeEqual_WhenEndLanesDiffer()
    {
        // Arrange
        var a = LaneRangeFactory.Create(startLane: 1, endLane: 10);
        var b = LaneRangeFactory.Create(startLane: 1, endLane: 12);

        // Act & Assert
        a.ShouldNotBe(b);
    }

    [Fact(DisplayName = "Two LaneRanges with different pin fall types are not equal")]
    public void Equality_ShouldNotBeEqual_WhenPinFallTypesDiffer()
    {
        // Arrange
        var a = LaneRangeFactory.Create(pinFallType: PinFallType.FreeFall);
        var b = LaneRangeFactory.Create(pinFallType: PinFallType.StringPin);

        // Act & Assert
        a.ShouldNotBe(b);
    }

    #endregion
}
