using Neba.Domain.BowlingCenters;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;

namespace Neba.Domain.Tests.BowlingCenters;

[UnitTest]
[Component("BowlingCenters")]
public sealed class LaneConfigurationTests
{
    #region Create — required ranges

#nullable disable
    [Fact(DisplayName = "Create returns error when ranges is null")]
    public void Create_ShouldReturnError_WhenRangesIsNull()
    {
        var result = LaneConfiguration.Create(null);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("LaneConfiguration.Ranges.Required");
    }
#nullable enable

    [Fact(DisplayName = "Create returns error when ranges is empty")]
    public void Create_ShouldReturnError_WhenRangesIsEmpty()
    {
        var result = LaneConfiguration.Create([]);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("LaneConfiguration.Ranges.Required");
    }

    #endregion

    #region Create — overlapping ranges

    [Fact(DisplayName = "Create returns error when ranges overlap")]
    public void Create_ShouldReturnError_WhenRangesOverlap()
    {
        var a = LaneRangeFactory.Create(startLane: 1, endLane: 10);
        var b = LaneRangeFactory.Create(startLane: 5, endLane: 14);

        var result = LaneConfiguration.Create([a, b]);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("LaneConfiguration.Ranges.Overlapping");
    }

    [Fact(DisplayName = "Create returns error when next range starts within previous range")]
    public void Create_ShouldReturnError_WhenRangesShareEndLane()
    {
        var a = LaneRangeFactory.Create(startLane: 1, endLane: 10);
        var b = LaneRangeFactory.Create(startLane: 9, endLane: 20);

        var result = LaneConfiguration.Create([a, b]);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("LaneConfiguration.Ranges.Overlapping");
    }

    #endregion

    #region Create — adjacent ranges

    [Fact(DisplayName = "Create returns error when adjacent ranges have the same pin fall type")]
    public void Create_ShouldReturnError_WhenAdjacentRangesHaveSamePinFallType()
    {
        var a = LaneRangeFactory.Create(startLane: 1, endLane: 10, pinFallType: PinFallType.FreeFall);
        var b = LaneRangeFactory.Create(startLane: 11, endLane: 20, pinFallType: PinFallType.FreeFall);

        var result = LaneConfiguration.Create([a, b]);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("LaneConfiguration.Ranges.Adjacent");
    }

    [Fact(DisplayName = "Create succeeds when adjacent ranges have different pin fall types")]
    public void Create_ShouldSucceed_WhenAdjacentRangesHaveDifferentPinFallTypes()
    {
        var a = LaneRangeFactory.Create(startLane: 1, endLane: 10, pinFallType: PinFallType.FreeFall);
        var b = LaneRangeFactory.Create(startLane: 11, endLane: 20, pinFallType: PinFallType.StringPin);

        var result = LaneConfiguration.Create([a, b]);

        result.IsError.ShouldBeFalse();
    }

    #endregion

    #region Create — valid

    [Fact(DisplayName = "Create succeeds with a single range")]
    public void Create_ShouldSucceed_WithSingleRange()
    {
        var range = LaneRangeFactory.Create();

        var result = LaneConfiguration.Create([range]);

        result.IsError.ShouldBeFalse();
        result.Value.Ranges.ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "Create succeeds with multiple non-adjacent ranges of the same type")]
    public void Create_ShouldSucceed_WithMultipleNonAdjacentRangesSameType()
    {
        // Gap at lanes 23-26 — classic split center scenario
        var a = LaneRangeFactory.Create(startLane: 1, endLane: 22, pinFallType: PinFallType.FreeFall);
        var b = LaneRangeFactory.Create(startLane: 27, endLane: 60, pinFallType: PinFallType.FreeFall);

        var result = LaneConfiguration.Create([a, b]);

        result.IsError.ShouldBeFalse();
        result.Value.Ranges.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Create sorts ranges by start lane")]
    public void Create_ShouldSortRanges_ByStartLane()
    {
        var a = LaneRangeFactory.Create(startLane: 27, endLane: 60);
        var b = LaneRangeFactory.Create(startLane: 1, endLane: 22);

        var result = LaneConfiguration.Create([a, b]);

        result.IsError.ShouldBeFalse();
        result.Value.Ranges.ElementAt(0).StartLane.ShouldBe(1);
        result.Value.Ranges.ElementAt(1).StartLane.ShouldBe(27);
    }

    #endregion

    #region TotalPairCount

    [Fact(DisplayName = "TotalPairCount returns pair count for single range")]
    public void TotalPairCount_ShouldReturnRangePairCount_ForSingleRange()
    {
        var config = LaneConfigurationFactory.Create([LaneRangeFactory.Create(startLane: 1, endLane: 10)]);

        config.TotalPairCount.ShouldBe(5);
    }

    [Fact(DisplayName = "TotalPairCount returns sum of pair counts across all ranges")]
    public void TotalPairCount_ShouldReturnSumOfPairCounts_AcrossAllRanges()
    {
        var a = LaneRangeFactory.Create(startLane: 1, endLane: 10);   // 5 pairs
        var b = LaneRangeFactory.Create(startLane: 13, endLane: 20);  // 4 pairs

        var config = LaneConfigurationFactory.Create([a, b]);

        config.TotalPairCount.ShouldBe(9);
    }

    #endregion

    #region Record equality

    [Fact(DisplayName = "Two LaneConfigurations with identical ranges are equal")]
    public void Equality_ShouldBeEqual_WhenRangesAreIdentical()
    {
        var range = LaneRangeFactory.Create();
        var a = LaneConfigurationFactory.Create([range]);
        var b = LaneConfigurationFactory.Create([range]);

        a.ShouldBe(b);
    }

    [Fact(DisplayName = "Two LaneConfigurations with different ranges are not equal")]
    public void Equality_ShouldNotBeEqual_WhenRangesDiffer()
    {
        var a = LaneConfigurationFactory.Create([LaneRangeFactory.Create(startLane: 1, endLane: 10)]);
        var b = LaneConfigurationFactory.Create([LaneRangeFactory.Create(startLane: 1, endLane: 20)]);

        a.ShouldNotBe(b);
    }

    #endregion
}
