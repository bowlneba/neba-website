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
        var responses = HallOfFameInductionResponseFactory.Bogus(3, seed: 1);

        var viewModels = responses.Select(r => r.ToViewModel()).ToList();

        await Verify(viewModels);
    }
}