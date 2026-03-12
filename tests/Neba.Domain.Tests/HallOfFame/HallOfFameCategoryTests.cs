using Ardalis.SmartEnum;

using Neba.Domain.HallOfFame;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.HallOfFame;

[UnitTest]
[Component("HallOfFame.HallOfFameCategory")]
public sealed class HallOfFameCategoryTests
{
    [Fact(DisplayName = "Should have 4 Hall of Fame categories")]
    public void HallOfFameCategory_ShouldHave4Categories()
    {
        // Act
        var count = HallOfFameCategory.List.Count;

        // Assert
        count.ShouldBe(3);
    }

    [Theory(DisplayName = "Hall of Fame category values should be correct")]
    [InlineData("Superior Performance", 1, TestDisplayName = "Superior Performance value should be 1")]
    [InlineData("Meritorious Service", 2, TestDisplayName = "Meritorious Service value should be 2")]
    [InlineData("Friend of NEBA", 4, TestDisplayName = "Friend of NEBA value should be 4")]
    public void HallOfFameCategory_ShouldHaveCorrectProperties(string expectedName, int expectedValue)
    {
        // Act
        var category = SmartFlagEnum<HallOfFameCategory>.FromValue(expectedValue).First();

        // Assert
        category.Name.ShouldBe(expectedName);
        category.Value.ShouldBe(expectedValue);
    }

    [Theory(DisplayName = "Combined category values should resolve to correct constituent flags")]
    [InlineData(3, new[] { "Superior Performance", "Meritorious Service" }, TestDisplayName = "SuperiorPerformance | MeritoriousService resolves to both flags")]
    [InlineData(5, new[] { "Superior Performance", "Friend of NEBA" }, TestDisplayName = "SuperiorPerformance | FriendOfNeba resolves to both flags")]
    [InlineData(6, new[] { "Meritorious Service", "Friend of NEBA" }, TestDisplayName = "MeritoriousService | FriendOfNeba resolves to both flags")]
    [InlineData(7, new[] { "Superior Performance", "Meritorious Service", "Friend of NEBA" }, TestDisplayName = "All three flags combined resolves to all three")]
    public void HallOfFameCategory_ShouldResolveCorrectFlags_WhenValueIsCombined(int combinedValue, string[] expectedNames)
    {
        ArgumentNullException.ThrowIfNull(expectedNames);

        // Act
        var flags = SmartFlagEnum<HallOfFameCategory>.FromValue(combinedValue).ToList();

        // Assert
        flags.Count.ShouldBe(expectedNames.Length);
        foreach (var name in expectedNames)
        {
            flags.ShouldContain(f => f.Name == name);
        }
    }
}