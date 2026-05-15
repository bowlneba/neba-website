using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.Domain;

[UnitTest]
[Component("Tournaments.PatternRatioCategory")]
public sealed class PatternRatioCategoryTests
{
    [Fact(DisplayName = "Should have 3 pattern ratio categories")]
    public void PatternRatioCategory_ShouldHave3Categories()
    {
        // Act
        var count = PatternRatioCategory.List.Count;

        // Assert
        count.ShouldBe(3);
    }

    [Theory(DisplayName = "Pattern ratio category properties should be correct")]
    [InlineData("Sport", 1, null, 4.0, TestDisplayName = "Sport should have value 1, no minimum, maximum 4.0")]
    [InlineData("Challenge", 2, 4.0, 8.0, TestDisplayName = "Challenge should have value 2, minimum 4.0, maximum 8.0")]
    [InlineData("Recreation", 3, 8.0, null, TestDisplayName = "Recreation should have value 3, minimum 8.0, no maximum")]
    public void PatternRatioCategory_ShouldHaveCorrectProperties(string expectedName, int expectedValue, double? expectedMinimum, double? expectedMaximum)
    {
        // Act
        var category = PatternRatioCategory.FromValue(expectedValue);

        // Assert
        category.Name.ShouldBe(expectedName);
        category.Value.ShouldBe(expectedValue);
        category.MinimumRatio.ShouldBe(expectedMinimum is null ? null : (decimal?)expectedMinimum.Value);
        category.MaximumRatio.ShouldBe(expectedMaximum is null ? null : (decimal?)expectedMaximum.Value);
    }
}