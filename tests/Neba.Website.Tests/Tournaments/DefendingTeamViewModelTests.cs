using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments;

namespace Neba.Website.Tests.Tournaments;

[UnitTest]
[Component("Website.Tournaments.DefendingTeamViewModel")]
public sealed class DefendingTeamViewModelTests
{
    [Theory(DisplayName = "IsRepeatChampion should be true only when the team won more than one tournament in a row")]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(5, true)]
    public void IsRepeatChampion_ShouldBeTrue_OnlyForMoreThanOneTitle(int titles, bool expected)
    {
        // Arrange
        var team = DefendingTeamViewModelFactory.Create(consecutiveTitles: titles);

        // Act
        var result = team.IsRepeatChampion;

        // Assert
        result.ShouldBe(expected);
    }

    [Fact(DisplayName = "TitleCountLabel should show the count with an x")]
    public void TitleCountLabel_ShouldShowCountWithX()
    {
        // Arrange
        var team = DefendingTeamViewModelFactory.Create(consecutiveTitles: 3);

        // Act
        var label = team.TitleCountLabel;

        // Assert
        label.ShouldBe("3x");
    }

    [Theory(DisplayName = "TitleCountDescription should read the count as a plural or singular number of titles in a row")]
    [InlineData(1, "1 title in a row")]
    [InlineData(2, "2 titles in a row")]
    [InlineData(4, "4 titles in a row")]
    public void TitleCountDescription_ShouldPluralize(int titles, string expected)
    {
        // Arrange
        var team = DefendingTeamViewModelFactory.Create(consecutiveTitles: titles);

        // Act
        var description = team.TitleCountDescription;

        // Assert
        description.ShouldBe(expected);
    }

    [Fact(DisplayName = "ToViewModel should keep the consecutive titles of the response")]
    public void ToViewModel_ShouldKeepConsecutiveTitles()
    {
        // Arrange
        var response = DefendingTeamResponseFactory.Create(consecutiveTitles: 2);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.ConsecutiveTitles.ShouldBe(2);
        viewModel.IsRepeatChampion.ShouldBeTrue();
    }
}