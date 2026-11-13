using Neba.Api.Features.HallOfFame;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.HallOfFame;

[UnitTest]
[Component("HallOfFame")]
public sealed class HallOfFamePointsMetricsTests
{
    [Theory(DisplayName = "RecordCacheMiss should add one measurement tagged with the query")]
    [InlineData(HallOfFamePointsMetrics.ListQuery, TestDisplayName = "list query")]
    [InlineData(HallOfFamePointsMetrics.DetailQuery, TestDisplayName = "detail query")]
    public void RecordCacheMiss_ShouldRecordMeasurement_WithQueryTag(string query)
    {
        // Arrange
        using var recorder = new CacheMissRecorder();

        // Act
        HallOfFamePointsMetrics.RecordCacheMiss(query);

        // Assert
        recorder.Queries.ShouldContain(query);
    }

    [Fact(DisplayName = "Meter and counter names should be stable")]
    public void Names_ShouldBeStable()
    {
        // Assert
        HallOfFamePointsMetrics.MeterName.ShouldBe("Neba.HallOfFame");
        HallOfFamePointsMetrics.CacheMissCountName.ShouldBe("neba.hall_of_fame.points.cache_miss.count");
    }
}