using Neba.TestFactory.Attributes;
using Neba.TestFactory.Awards;
using Neba.Website.Server.History.Awards;

namespace Neba.Website.Tests.History.Awards;

[UnitTest]
[Component("Website.History.Awards.HighAverageMappingExtensions")]
public sealed class HighAverageMappingExtensionsTests
{
    [Fact(DisplayName = "Maps Season from response to view model")]
    public void ToViewModel_ShouldMapSeason()
    {
        var response = HighAverageAwardResponseFactory.Create(season: "2024 Season");

        var vm = response.ToViewModel();

        vm.Season.ShouldBe("2024 Season");
    }

    [Fact(DisplayName = "Maps BowlerName from response to view model")]
    public void ToViewModel_ShouldMapBowlerName()
    {
        var response = HighAverageAwardResponseFactory.Create(bowlerName: "Jane Smith");

        var vm = response.ToViewModel();

        vm.BowlerName.ShouldBe("Jane Smith");
    }

    [Fact(DisplayName = "Maps Average from response to view model")]
    public void ToViewModel_ShouldMapAverage()
    {
        var response = HighAverageAwardResponseFactory.Create(average: 228.75m);

        var vm = response.ToViewModel();

        vm.Average.ShouldBe(228.75m);
    }

    [Fact(DisplayName = "Maps TotalGames from response to view model when present")]
    public void ToViewModel_ShouldMapTotalGames_WhenPresent()
    {
        var response = HighAverageAwardResponseFactory.Create(totalGames: 63);

        var vm = response.ToViewModel();

        vm.TotalGames.ShouldBe(63);
    }

    [Fact(DisplayName = "Maps TotalGames as null when not present in response")]
    public void ToViewModel_ShouldMapTotalGames_WhenNull()
    {
        var response = HighAverageAwardResponseFactory.Create(totalGames: null);

        var vm = response.ToViewModel();

        vm.TotalGames.ShouldBeNull();
    }

    [Fact(DisplayName = "Maps TournamentsParticipated from response to view model when present")]
    public void ToViewModel_ShouldMapTournamentsParticipated_WhenPresent()
    {
        var response = HighAverageAwardResponseFactory.Create(tournamentsParticipated: 14);

        var vm = response.ToViewModel();

        vm.TournamentsParticipated.ShouldBe(14);
    }

    [Fact(DisplayName = "Maps TournamentsParticipated as null when not present in response")]
    public void ToViewModel_ShouldMapTournamentsParticipated_WhenNull()
    {
        var response = HighAverageAwardResponseFactory.Create(tournamentsParticipated: null);

        var vm = response.ToViewModel();

        vm.TournamentsParticipated.ShouldBeNull();
    }
}
