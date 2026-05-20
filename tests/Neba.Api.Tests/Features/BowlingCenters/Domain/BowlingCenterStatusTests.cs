using Neba.Api.Features.BowlingCenters.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.BowlingCenters.Domain;

[UnitTest]
[Component("BowlingCenters.BowlingCenterStatus")]
public sealed class BowlingCenterStatusTests
{
    [Fact(DisplayName = "Should have 3 bowling center statuses")]
    public void BowlingCenterStatus_ShouldHave3Statuses()
    {
        // Act
        var count = BowlingCenterStatus.List.Count;

        // Assert
        count.ShouldBe(3);
    }

    [Theory(DisplayName = "Bowling center status values should be correct")]
    [InlineData("Open", 0, TestDisplayName = "Open value should be 0")]
    [InlineData("Closed", 1, TestDisplayName = "Closed value should be 1")]
    [InlineData("Uncertified", 2, TestDisplayName = "Uncertified value should be 2")]
    public void BowlingCenterStatus_ShouldHaveCorrectProperties(string expectedName, int expectedValue)
    {
        // Act
        var status = BowlingCenterStatus.FromValue(expectedValue);

        // Assert
        status.Name.ShouldBe(expectedName);
        status.Value.ShouldBe(expectedValue);
    }
}