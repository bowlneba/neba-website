using Neba.Domain.Sponsors;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.Sponsors;

[UnitTest]
[Component("Sponsors.SponsorCategory")]
public sealed class SponsorCategoryTests
{
    [Fact(DisplayName = "Should have 8 sponsor categories")]
    public void SponsorCategory_ShouldHave8Categories()
    {
        // Act
        var count = SponsorCategory.List.Count;

        // Assert
        count.ShouldBe(8);
    }

    [Theory(DisplayName = "Sponsor category properties should be correct")]
    [InlineData("Manufacturer", 1, TestDisplayName = "Manufacturer should have value 1")]
    [InlineData("Pro Shop", 2, TestDisplayName = "Pro Shop should have value 2")]
    [InlineData("Bowling Center", 3, TestDisplayName = "Bowling Center should have value 3")]
    [InlineData("Financial Services", 4, TestDisplayName = "Financial Services should have value 4")]
    [InlineData("Technology", 5, TestDisplayName = "Technology should have value 5")]
    [InlineData("Media", 6, TestDisplayName = "Media should have value 6")]
    [InlineData("Individual", 100, TestDisplayName = "Individual should have value 100")]
    [InlineData("Other", 999, TestDisplayName = "Other should have value 999")]
    public void SponsorCategory_ShouldHaveCorrectProperties(string categoryName, int expectedValue)
    {
        // Act
        var category = SponsorCategory.FromValue(expectedValue);

        // Assert
        category.Name.ShouldBe(categoryName);
        category.Value.ShouldBe(expectedValue);
    }
}