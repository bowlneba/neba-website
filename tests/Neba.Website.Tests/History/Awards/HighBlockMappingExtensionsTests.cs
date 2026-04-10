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
        var responses = HighBlockAwardResponseFactory.Bogus(3, seed: 1);

        var viewModels = responses.ToViewModels();

        await Verify(viewModels);
    }

    [Fact(DisplayName = "Maps single award to single bowler view model")]
    public void ToViewModels_ShouldMapSingleAward_ToSingleBowlerViewModel()
    {
        var response = HighBlockAwardResponseFactory.Create(season: "2024 Season", bowlerName: "Jane Smith", score: 1350);

        var result = new[] { response }.ToViewModels();

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
        var award1 = HighBlockAwardResponseFactory.Create(season: "2023 Season", bowlerName: "Alice", score: 1375);
        var award2 = HighBlockAwardResponseFactory.Create(season: "2023 Season", bowlerName: "Bob", score: 1375);

        var result = new[] { award1, award2 }.ToViewModels();

        result.ShouldHaveSingleItem();
        var vm = result.Single();
        vm.Bowlers.Count.ShouldBe(2);
        vm.Bowlers.ShouldContain("Alice");
        vm.Bowlers.ShouldContain("Bob");
    }

    [Fact(DisplayName = "Does not group awards with same season but different scores")]
    public void ToViewModels_ShouldNotGroup_WhenSameSeasonDifferentScore()
    {
        var award1 = HighBlockAwardResponseFactory.Create(season: "2023 Season", bowlerName: "Alice", score: 1375);
        var award2 = HighBlockAwardResponseFactory.Create(season: "2023 Season", bowlerName: "Bob", score: 1350);

        var result = new[] { award1, award2 }.ToViewModels();

        result.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Returns empty collection when input is empty")]
    public void ToViewModels_ShouldReturnEmpty_WhenInputIsEmpty()
    {
        var result = Array.Empty<HighBlockAwardResponse>().ToViewModels();

        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Maps season and score correctly for each distinct award")]
    public void ToViewModels_ShouldMapSeasonAndScore_ForEachDistinctAward()
    {
        var award1 = HighBlockAwardResponseFactory.Create(season: "2022 Season", bowlerName: "Alice", score: 1300);
        var award2 = HighBlockAwardResponseFactory.Create(season: "2023 Season", bowlerName: "Bob", score: 1350);

        var result = new[] { award1, award2 }.ToViewModels();

        result.Count.ShouldBe(2);
        result.ShouldContain(vm => vm.Season == "2022 Season" && vm.Score == 1300);
        result.ShouldContain(vm => vm.Season == "2023 Season" && vm.Score == 1350);
    }
}