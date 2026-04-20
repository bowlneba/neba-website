using Neba.Domain.Tournaments;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.Tournaments;

[UnitTest]
[Component("Tournaments.PatternLengthCategory")]
public sealed class PatternLengthCategoryTests
{
    [Fact(DisplayName = "Should have 3 pattern length categories")]
    public void PatternLengthCategory_ShouldHave3Categories()
    {
        // Act
        var count = PatternLengthCategory.List.Count;

        // Assert
        count.ShouldBe(3);
    }

    [Theory(DisplayName = "Pattern length category properties should be correct")]
    [InlineData("Short", 1, null, 37, TestDisplayName = "Short should have value 1, no minimum, maximum 37")]
    [InlineData("Medium", 2, 38, 42, TestDisplayName = "Medium should have value 2, minimum 38, maximum 42")]
    [InlineData("Long", 3, 43, null, TestDisplayName = "Long should have value 3, minimum 43, no maximum")]
    public void PatternLengthCategory_ShouldHaveCorrectProperties(string expectedName, int expectedValue, int? expectedMinimum, int? expectedMaximum)
    {
        // Act
        var category = PatternLengthCategory.FromValue(expectedValue);

        // Assert
        category.Name.ShouldBe(expectedName);
        category.Value.ShouldBe(expectedValue);
        category.MinimumLength.ShouldBe(expectedMinimum);
        category.MaximumLength.ShouldBe(expectedMaximum);
    }
}
