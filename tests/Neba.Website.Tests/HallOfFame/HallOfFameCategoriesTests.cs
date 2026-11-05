using Neba.Api.Features.HallOfFame.Domain;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.HallOfFame;

namespace Neba.Website.Tests.HallOfFame;

[UnitTest]
[Component("Website.HallOfFame.HallOfFameCategories")]
public sealed class HallOfFameCategoriesTests
{
    [Fact(DisplayName = "Should list the same category names the API sends, in display order")]
    public void Names_ShouldMatchDomainCategoryNames_InDisplayOrder()
    {
        // Arrange
        // Act
        var names = HallOfFameCategories.Names;

        // Assert
        names.ShouldBe(
        [
            HallOfFameCategory.SuperiorPerformance.Name,
            HallOfFameCategory.MeritoriousService.Name,
            HallOfFameCategory.FriendOfNeba.Name
        ]);
    }

    [Fact(DisplayName = "Should order known categories by position and unknown categories last")]
    public void OrderOf_ShouldSortUnknownLast()
    {
        // Arrange
        // Act
        var known = HallOfFameCategories.OrderOf(HallOfFameCategory.MeritoriousService.Name);
        var unknown = HallOfFameCategories.OrderOf("Something Else");

        // Assert
        known.ShouldBe(1);
        unknown.ShouldBe(3);
    }

    [Theory(DisplayName = "Should give each known category its own dot class and unknown ones none")]
    [InlineData("Superior Performance", "hof-dot--superior")]
    [InlineData("Meritorious Service", "hof-dot--meritorious")]
    [InlineData("Friend of NEBA", "hof-dot--friend")]
    [InlineData("Something Else", "")]
    public void DotClass_ShouldMapCategory(string category, string expected)
    {
        // Arrange
        // Act
        var result = HallOfFameCategories.DotClass(category);

        // Assert
        result.ShouldBe(expected);
    }
}
