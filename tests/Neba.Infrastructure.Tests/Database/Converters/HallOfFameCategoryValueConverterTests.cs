using Neba.Domain.HallOfFame;
using Neba.Infrastructure.Database.Converters;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.Database.Converters;

[UnitTest]
[Component("Database.Converters")]
public sealed class HallOfFameCategoryValueConverterTests
{
    [Fact(DisplayName = "Should convert categories to combined bit flag value")]
    public void ConvertToProvider_ShouldReturnCombinedFlagValue_WhenMultipleCategoriesProvided()
    {
        // Arrange
        IReadOnlyCollection<HallOfFameCategory> categories =
        [
            HallOfFameCategory.SuperiorPerformance,
            HallOfFameCategory.FriendOfNeba,
        ];
        var converter = new HallOfFameCategoryValueConverter();

        // Act
        var result = (int)converter.ConvertToProvider(categories)!;

        // Assert
        result.ShouldBe(
            HallOfFameCategory.SuperiorPerformance.Value |
            HallOfFameCategory.FriendOfNeba.Value);
    }

    [Fact(DisplayName = "Should convert empty categories to zero")]
    public void ConvertToProvider_ShouldReturnZero_WhenCategoriesAreEmpty()
    {
        // Arrange
        IReadOnlyCollection<HallOfFameCategory> categories = [];
        var converter = new HallOfFameCategoryValueConverter();

        // Act
        var result = (int)converter.ConvertToProvider(categories)!;

        // Assert
        result.ShouldBe(0);
    }

    [Fact(DisplayName = "Should convert bit flag value to matching categories")]
    public void ConvertFromProvider_ShouldReturnMatchingCategories_WhenValueContainsMultipleFlags()
    {
        // Arrange
        var providerValue =
            HallOfFameCategory.MeritoriousService.Value |
            HallOfFameCategory.FriendOfNeba.Value;
        var converter = new HallOfFameCategoryValueConverter();

        // Act
        var result = (IReadOnlyCollection<HallOfFameCategory>)converter.ConvertFromProvider(providerValue)!;

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(HallOfFameCategory.MeritoriousService);
        result.ShouldContain(HallOfFameCategory.FriendOfNeba);
    }

    [Fact(DisplayName = "Should preserve categories when converting to and from provider")]
    public void RoundTrip_ShouldPreserveCategories_WhenConvertingToAndFromProvider()
    {
        // Arrange
        IReadOnlyCollection<HallOfFameCategory> originalCategories =
        [
            HallOfFameCategory.SuperiorPerformance,
            HallOfFameCategory.MeritoriousService,
            HallOfFameCategory.FriendOfNeba,
        ];
        var converter = new HallOfFameCategoryValueConverter();

        // Act
        var providerValue = (int)converter.ConvertToProvider(originalCategories)!;
        var roundTripCategories = (IReadOnlyCollection<HallOfFameCategory>)converter.ConvertFromProvider(providerValue)!;

        // Assert
        roundTripCategories.Count.ShouldBe(originalCategories.Count);
        foreach (var category in originalCategories)
        {
            roundTripCategories.ShouldContain(category);
        }
    }
}