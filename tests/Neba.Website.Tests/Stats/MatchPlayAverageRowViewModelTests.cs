using Neba.TestFactory.Attributes;
using Neba.TestFactory.Stats;

namespace Neba.Website.Tests.Stats;

[UnitTest]
[Component("Website.Stats.MatchPlayAverageRow")]
public sealed class MatchPlayAverageRowViewModelTests
{
    [Fact(DisplayName = "WinPercentage should be wins over wins plus losses, not over games")]
    public void WinPercentage_ShouldUseWinsAndLosses_NotGames()
    {
        // Arrange
        // Games excludes Baker games, while the record includes Baker finals
        var row = MatchPlayAverageRowViewModelFactory.Create(games: 12, wins: 12, loses: 3);

        // Act
        var result = row.WinPercentage;

        // Assert
        result.ShouldBe(80m);
    }

    [Fact(DisplayName = "WinPercentage should be null when there are no wins or losses")]
    public void WinPercentage_ShouldBeNull_WhenNoWinsOrLosses()
    {
        // Arrange
        var row = MatchPlayAverageRowViewModelFactory.Create(games: 10, wins: 0, loses: 0);

        // Act
        var result = row.WinPercentage;

        // Assert
        result.ShouldBeNull();
    }
}