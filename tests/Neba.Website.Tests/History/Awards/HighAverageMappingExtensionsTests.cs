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
        // Arrange
        var responses = HighAverageAwardResponseFactory.Bogus(3, seed: 1);

        // Act
        var viewModels = responses.Select(r => r.ToViewModel()).ToList();

        // Assert
        await Verify(viewModels);
    }

    [Fact(DisplayName = "Maps TotalGames as null when not present in response")]
    public void ToViewModel_ShouldMapTotalGames_WhenNull()
    {
        // Arrange
        var response = HighAverageAwardResponseFactory.Create(totalGames: null);

        // Act
        var vm = response.ToViewModel();

        // Assert
        vm.TotalGames.ShouldBeNull();
    }

    [Fact(DisplayName = "Maps TournamentsParticipated as null when not present in response")]
    public void ToViewModel_ShouldMapTournamentsParticipated_WhenNull()
    {
        // Arrange
        var response = HighAverageAwardResponseFactory.Create(tournamentsParticipated: null);

        // Act
        var vm = response.ToViewModel();

        // Assert
        vm.TournamentsParticipated.ShouldBeNull();
    }
}
