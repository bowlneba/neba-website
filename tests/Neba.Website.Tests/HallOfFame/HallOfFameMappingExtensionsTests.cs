using Neba.Domain.HallOfFame;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;
using Neba.Website.Server.HallOfFame;

namespace Neba.Website.Tests.HallOfFame;

[UnitTest]
[Component("Website.HallOfFame.HallOfFameMappingExtensions")]
public sealed class HallOfFameMappingExtensionsTests
{
    [Fact(DisplayName = "ToViewModel should map BowlerName")]
    public void ToViewModel_ShouldMapBowlerName()
    {
        var dto = HallOfFameInductionResponseFactory.Create(bowlerName: "Alice Jones");

        var vm = dto.ToViewModel();

        vm.BowlerName.ShouldBe("Alice Jones");
    }

    [Fact(DisplayName = "ToViewModel should map Year to InductionYear")]
    public void ToViewModel_ShouldMapYear()
    {
        var dto = HallOfFameInductionResponseFactory.Create(year: 2018);

        var vm = dto.ToViewModel();

        vm.InductionYear.ShouldBe(2018);
    }

    [Fact(DisplayName = "ToViewModel should map Categories")]
    public void ToViewModel_ShouldMapCategories()
    {
        var categories = new[] { HallOfFameCategory.SuperiorPerformance.Name, HallOfFameCategory.MeritoriousService.Name };
        var dto = HallOfFameInductionResponseFactory.Create(categories: categories);

        var vm = dto.ToViewModel();

        vm.Categories.ShouldBe(categories);
    }

    [Fact(DisplayName = "ToViewModel should map PhotoUri when present")]
    public void ToViewModel_ShouldMapPhotoUri_WhenPresent()
    {
        var photoUri = new Uri("https://example.com/photo.jpg");
        var dto = HallOfFameInductionResponseFactory.Create(photoUri: photoUri);

        var vm = dto.ToViewModel();

        vm.PhotoUri.ShouldBe(photoUri);
    }

    [Fact(DisplayName = "ToViewModel should map null PhotoUri as null")]
    public void ToViewModel_ShouldMapPhotoUri_WhenNull()
    {
        var dto = HallOfFameInductionResponseFactory.Create(photoUri: null);

        var vm = dto.ToViewModel();

        vm.PhotoUri.ShouldBeNull();
    }
}