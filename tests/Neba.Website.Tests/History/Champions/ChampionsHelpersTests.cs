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
}
