using Neba.TestFactory.Attributes;
using Neba.Website.Server.History.Champions;

namespace Neba.Website.Tests.History.Champions;

[UnitTest]
[Component("Website.History.Champions.ChampionsHelpers")]
public sealed class ChampionsHelpersTests
{
    // ── MonthAbbreviation ─────────────────────────────────────────────────

    [Theory(DisplayName = "MonthAbbreviation should return correct 3-letter abbreviation")]
    [InlineData(1, "Jan")]
    [InlineData(2, "Feb")]
    [InlineData(3, "Mar")]
    [InlineData(4, "Apr")]
    [InlineData(5, "May")]
    [InlineData(6, "Jun")]
    [InlineData(7, "Jul")]
    [InlineData(8, "Aug")]
    [InlineData(9, "Sep")]
    [InlineData(10, "Oct")]
    [InlineData(11, "Nov")]
    [InlineData(12, "Dec")]
    public void MonthAbbreviation_ShouldReturnCorrectAbbreviation(int month, string expected)
    {
        // Arrange & Act
        var result = ChampionsHelpers.MonthAbbreviation(month);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact(DisplayName = "MonthAbbreviation should return empty string for out-of-range month")]
    public void MonthAbbreviation_ShouldReturnEmpty_WhenMonthIsOutOfRange()
    {
        // Arrange & Act
        var result = ChampionsHelpers.MonthAbbreviation(0);

        // Assert
        result.ShouldBe(string.Empty);
    }

    // ── TypePillClass ─────────────────────────────────────────────────────

    [Fact(DisplayName = "TypePillClass should return type-doubles for tournament type containing 'double'")]
    public void TypePillClass_ShouldReturnTypeDoubles_WhenTypeContainsDouble()
    {
        // Arrange & Act
        var result = ChampionsHelpers.TypePillClass("Doubles Classic");

        // Assert
        result.ShouldBe("type-doubles");
    }

    [Fact(DisplayName = "TypePillClass should return type-trios for tournament type containing 'trio'")]
    public void TypePillClass_ShouldReturnTypeTrios_WhenTypeContainsTrio()
    {
        // Arrange & Act
        var result = ChampionsHelpers.TypePillClass("Trios Championship");

        // Assert
        result.ShouldBe("type-trios");
    }

    [Fact(DisplayName = "TypePillClass should return type-team for tournament type containing 'team'")]
    public void TypePillClass_ShouldReturnTypeTeam_WhenTypeContainsTeam()
    {
        // Arrange & Act
        var result = ChampionsHelpers.TypePillClass("Team Event");

        // Assert
        result.ShouldBe("type-team");
    }

    [Fact(DisplayName = "TypePillClass should return type-senior for tournament type containing 'senior'")]
    public void TypePillClass_ShouldReturnTypeSenior_WhenTypeContainsSenior()
    {
        // Arrange & Act
        var result = ChampionsHelpers.TypePillClass("Senior Singles");

        // Assert
        result.ShouldBe("type-senior");
    }

    [Fact(DisplayName = "TypePillClass should return type-women for tournament type containing 'women'")]
    public void TypePillClass_ShouldReturnTypeWomen_WhenTypeContainsWomen()
    {
        // Arrange & Act
        var result = ChampionsHelpers.TypePillClass("Women's Singles");

        // Assert
        result.ShouldBe("type-women");
    }

    [Fact(DisplayName = "TypePillClass should return type-special for tournament type containing 'special'")]
    public void TypePillClass_ShouldReturnTypeSpecial_WhenTypeContainsSpecial()
    {
        // Arrange & Act
        var result = ChampionsHelpers.TypePillClass("Special Event");

        // Assert
        result.ShouldBe("type-special");
    }

    [Fact(DisplayName = "TypePillClass should return type-singles as default for unrecognised tournament type")]
    public void TypePillClass_ShouldReturnTypeSingles_WhenTypeIsUnrecognised()
    {
        // Arrange & Act
        var result = ChampionsHelpers.TypePillClass("Singles");

        // Assert
        result.ShouldBe("type-singles");
    }

    [Fact(DisplayName = "TypePillClass matching is case-insensitive")]
    public void TypePillClass_ShouldBeCaseInsensitive()
    {
        // Arrange & Act
        var lower = ChampionsHelpers.TypePillClass("doubles");
        var upper = ChampionsHelpers.TypePillClass("DOUBLES");
        var mixed = ChampionsHelpers.TypePillClass("DoUbLeS");

        // Assert
        lower.ShouldBe("type-doubles");
        upper.ShouldBe("type-doubles");
        mixed.ShouldBe("type-doubles");
    }
}