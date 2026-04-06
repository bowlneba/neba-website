using Neba.TestFactory.Attributes;
using Neba.TestFactory.Awards;
using Neba.Website.Server.History.Awards;

namespace Neba.Website.Tests.History.Awards;

[UnitTest]
[Component("Website.History.Awards.HighAverageMappingExtensions")]
public sealed class HighAverageMappingExtensionsTests
{
    [Fact(DisplayName = "Maps all fields from response to view model")]
    public async Task ToViewModel_ShouldMapAllFields()
    {
        var responses = HighAverageAwardResponseFactory.Bogus(3, seed: 1);

        var viewModels = responses.Select(r => r.ToViewModel()).ToList();

        await Verify(viewModels);
    }

    [Fact(DisplayName = "Maps TotalGames as null when not present in response")]
    public void ToViewModel_ShouldMapTotalGames_WhenNull()
    {
        var response = HighAverageAwardResponseFactory.Create(totalGames: null);

        var vm = response.ToViewModel();

        vm.TotalGames.ShouldBeNull();
    }

    [Fact(DisplayName = "Maps TournamentsParticipated as null when not present in response")]
    public void ToViewModel_ShouldMapTournamentsParticipated_WhenNull()
    {
        var response = HighAverageAwardResponseFactory.Create(tournamentsParticipated: null);

        var vm = response.ToViewModel();

        vm.TournamentsParticipated.ShouldBeNull();
    }
}
