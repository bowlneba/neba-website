using Neba.Domain.Awards;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.Awards;

[UnitTest]
[Component("Awards.SeasonAwardType")]
public sealed class SeasonAwardTypeTests
{
    [Fact(DisplayName = "Should have 3 season award types")]
    public void SeasonAwardType_ShouldHave3Types()
    {
        // Act
        var count = SeasonAwardType.List.Count;

        // Assert
        count.ShouldBe(3);
    }

    [Theory(DisplayName = "Season award type values should be correct")]
    [InlineData("BowlerOfTheYear", 1, TestDisplayName = "Bowler of the Year season award type should have value 1")]
    [InlineData("HighAverage", 2, TestDisplayName = "High Average season award type should have value 2")]
    [InlineData("HighBlock", 3, TestDisplayName = "High Block season award type should have value 3")]
    public void SeasonAwardType_ShouldHaveCorrectValues(string name, int value)
    {
        // Act
        var seasonAwardType = SeasonAwardType.FromValue(value);

        // Assert
        seasonAwardType.Name.ShouldBe(name);
        seasonAwardType.Value.ShouldBe(value);
    }
}