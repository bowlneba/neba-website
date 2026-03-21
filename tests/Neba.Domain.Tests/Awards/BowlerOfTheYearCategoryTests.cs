using Neba.Domain.Awards;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.Awards;

[UnitTest]
[Component("Awards.BowlerOfTheYearCategory")]
public sealed class BowlerOfTheYearCategoryTests
{
    [Fact(DisplayName = "Should have 6 bowler of the year categories")]
    public void BowlerOfTheYearCategory_ShouldHave6Categories()
    {
        // Act
        var count = BowlerOfTheYearCategory.List.Count;

        // Assert
        count.ShouldBe(6);
    }

    [Theory(DisplayName = "Bowler of the year category values should be correct")]
    [InlineData("Open", 1, TestDisplayName = "Open category should have value 1")]
    [InlineData("Woman", 2, TestDisplayName = "Woman category should have value 2")]
    [InlineData("Senior", 3, TestDisplayName = "Senior category should have value 3")]
    [InlineData("Super Senior", 4, TestDisplayName = "Super Senior category should have value 4")]
    [InlineData("Rookie", 5, TestDisplayName = "Rookie category should have value 5")]
    [InlineData("Youth", 6, TestDisplayName = "Youth category should have value 6")]
    public void BowlerOfTheYearCategory_ShouldHaveCorrectValues(string name, int value)
    {
        // Act
        var bowlerOfTheYearCategory = BowlerOfTheYearCategory.FromValue(value);

        // Assert
        bowlerOfTheYearCategory.Name.ShouldBe(name);
        bowlerOfTheYearCategory.Value.ShouldBe(value);
    }
}