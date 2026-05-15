using Neba.Api.Features.Sponsors.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Sponsors.Domain;

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
    [InlineData("Other", 1, TestDisplayName = "Other should have value 1")]
    [InlineData("Manufacturer", 2, TestDisplayName = "Manufacturer should have value 2")]
    [InlineData("Pro Shop", 4, TestDisplayName = "Pro Shop should have value 4")]
    [InlineData("Bowling Center", 8, TestDisplayName = "Bowling Center should have value 8")]
    [InlineData("Financial Services", 16, TestDisplayName = "Financial Services should have value 16")]
    [InlineData("Technology", 32, TestDisplayName = "Technology should have value 32")]
    [InlineData("Media", 64, TestDisplayName = "Media should have value 64")]
    [InlineData("Individual", 128, TestDisplayName = "Individual should have value 128")]
    public void SponsorCategory_ShouldHaveCorrectProperties(string categoryName, int expectedValue)
    {
        // Act
        var category = SponsorCategory.FromValue(expectedValue);

        // Assert
        category.Name.ShouldBe(categoryName);
        category.Value.ShouldBe(expectedValue);
    }
}