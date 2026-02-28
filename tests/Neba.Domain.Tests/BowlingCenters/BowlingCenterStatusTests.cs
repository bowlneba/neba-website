using Neba.Domain.BowlingCenters;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.BowlingCenters;

[UnitTest]
[Component("BowlingCenters.BowlingCenterStatus")]
public sealed class BowlingCenterStatusTests
{
    [Fact(DisplayName = "Should have 2 bowling center statuses")]
    public void BowlingCenterStatus_ShouldHave2Statuses()
    {
        BowlingCenterStatus.List.Count.ShouldBe(2);
    }

    [Theory(DisplayName = "Bowling center status values should be correct")]
    [InlineData("Open", 0, TestDisplayName = "Open value should be 0")]
    [InlineData("Closed", 1, TestDisplayName = "Closed value should be 1")]
    public void BowlingCenterStatus_ShouldHaveCorrectProperties(string expectedName, int expectedValue)
    {
        var status = BowlingCenterStatus.FromValue(expectedValue);
        status.Name.ShouldBe(expectedName);
        status.Value.ShouldBe(expectedValue);
    }
}