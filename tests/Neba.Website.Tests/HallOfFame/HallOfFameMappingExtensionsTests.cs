using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;
using Neba.Website.Server.HallOfFame;

namespace Neba.Website.Tests.HallOfFame;

[UnitTest]
[Component("Website.HallOfFame.HallOfFameMappingExtensions")]
public sealed class HallOfFameMappingExtensionsTests
{
    [Fact(DisplayName = "Maps all fields from response to view model")]
    public async Task ToViewModel_ShouldMapAllFields()
    {
        // Arrange
        var responses = HallOfFameInductionResponseFactory.Bogus(3, seed: 1);

        // Act
        var viewModels = responses.Select(r => r.ToViewModel()).ToList();

        // Assert
        await Verify(viewModels);
    }
}