using Neba.Api.Contracts.Awards;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Awards;
using Neba.Website.Server.History.Awards;

namespace Neba.Website.Tests.History.Awards;

[UnitTest]
[Component("Website.History.Awards.HighBlockMappingExtensions")]
public sealed class HighBlockMappingExtensionsTests
{
    [Fact(DisplayName = "Maps all fields from response to view model")]
    public async Task ToViewModels_ShouldMapAllFields()
    {
        // Arrange
        var responses = HighBlockAwardResponseFactory.Bogus(3, seed: 1);

        // Act
        var viewModels = responses.ToViewModels();

        // Assert
        await Verify(viewModels);
    }

    [Fact(DisplayName = "Maps single award to single bowler view model")]
    public void ToViewModels_ShouldMapSingleAward_ToSingleBowlerViewModel()
    {
        // Arrange
        var response = HighBlockAwardResponseFactory.Create(season: "2024 Season", bowlerName: "Jane Smith", score: 1350);

        // Act
        var result = new[] { response }.ToViewModels();

        // Assert
        result.ShouldHaveSingleItem();
        var vm = result.Single();
        vm.Season.ShouldBe("2024 Season");
        vm.Score.ShouldBe(1350);
        vm.Bowlers.ShouldHaveSingleItem();
        vm.Bowlers.Single().ShouldBe("Jane Smith");
    }

    [Fact(DisplayName = "Groups tied awards with same season and score into one view model")]
    public void ToViewModels_ShouldGroupTiedAwards_WhenSameSeasonAndScore()
    {
        // Arrange
        var award1 = HighBlockAwardResponseFactory.Create(season: "2023 Season", bowlerName: "Alice", score: 1375);
        var award2 = HighBlockAwardResponseFactory.Create(season: "2023 Season", bowlerName: "Bob", score: 1375);

        // Act
        var result = new[] { award1, award2 }.ToViewModels();

        // Assert
        result.ShouldHaveSingleItem();
        var vm = result.Single();
        vm.Bowlers.Count.ShouldBe(2);
        vm.Bowlers.ShouldContain("Alice");
        vm.Bowlers.ShouldContain("Bob");
    }

    [Fact(DisplayName = "Does not group awards with same season but different scores")]
    public void ToViewModels_ShouldNotGroup_WhenSameSeasonDifferentScore()
    {
        // Arrange
        var award1 = HighBlockAwardResponseFactory.Create(season: "2023 Season", bowlerName: "Alice", score: 1375);
        var award2 = HighBlockAwardResponseFactory.Create(season: "2023 Season", bowlerName: "Bob", score: 1350);

        // Act
        var result = new[] { award1, award2 }.ToViewModels();

        // Assert
        result.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Returns empty collection when input is empty")]
    public void ToViewModels_ShouldReturnEmpty_WhenInputIsEmpty()
    {
        // Act
        var result = Array.Empty<HighBlockAwardResponse>().ToViewModels();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Maps season and score correctly for each distinct award")]
    public void ToViewModels_ShouldMapSeasonAndScore_ForEachDistinctAward()
    {
        // Arrange
        var award1 = HighBlockAwardResponseFactory.Create(season: "2022 Season", bowlerName: "Alice", score: 1300);
        var award2 = HighBlockAwardResponseFactory.Create(season: "2023 Season", bowlerName: "Bob", score: 1350);

        // Act
        var result = new[] { award1, award2 }.ToViewModels();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(vm => vm.Season == "2022 Season" && vm.Score == 1300);
        result.ShouldContain(vm => vm.Season == "2023 Season" && vm.Score == 1350);
    }
}
