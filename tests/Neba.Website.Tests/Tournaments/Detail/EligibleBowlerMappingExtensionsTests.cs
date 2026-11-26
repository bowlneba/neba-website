using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments.Detail;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.EligibleBowlerMappingExtensions")]
public sealed class EligibleBowlerMappingExtensionsTests
{
    [Fact(DisplayName = "Maps the requirement and the bowlers in order")]
    public void ToViewModel_ShouldMapFields_WhenGivenResponse()
    {
        // Arrange
        var response = EligibleBowlersResponseFactory.Create(
            requirements: EligibilityRequirementsResponseFactory.Create(minTournamentsCount: 5),
            bowlers:
            [
                EligibleBowlerSummaryResponseFactory.Create(name: "Zed Young", usbcId: "2222-22222"),
                EligibleBowlerSummaryResponseFactory.Create(name: "Ann Adams", usbcId: null)
            ]);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.ListApplies.ShouldBeTrue();
        viewModel.MinTournamentsCount.ShouldBe(5);
        viewModel.Bowlers.Select(b => b.Name).ShouldBe(["Zed Young", "Ann Adams"]);
        viewModel.Bowlers.Select(b => b.UsbcId).ShouldBe(["2222-22222", null]);
    }

    [Fact(DisplayName = "Maps null requirements to a list that does not apply")]
    public void ToViewModel_ShouldNotApply_WhenRequirementsAreNull()
    {
        // Arrange
        var response = EligibleBowlersResponseFactory.CreateNotApplicable();

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.ListApplies.ShouldBeFalse();
        viewModel.MinTournamentsCount.ShouldBeNull();
        viewModel.Bowlers.ShouldBeEmpty();
    }

    [Theory(DisplayName = "Builds a lower-case name key and a USBC ID key without dashes or spaces")]
    [InlineData("4821-09317", "482109317", TestDisplayName = "Dash")]
    [InlineData("48 21-09317", "482109317", TestDisplayName = "Space and dash")]
    [InlineData(null, "", TestDisplayName = "No ID")]
    public void ToViewModel_ShouldBuildSearchKeys_WhenMapped(string? usbcId, string expectedKey)
    {
        // Arrange
        var response = EligibleBowlersResponseFactory.Create(
            bowlers: [EligibleBowlerSummaryResponseFactory.Create(name: "Pat O'Brien", usbcId: usbcId)]);

        // Act
        var bowler = response.ToViewModel().Bowlers.ShouldHaveSingleItem();

        // Assert
        bowler.NameKey.ShouldBe("pat o'brien");
        bowler.UsbcKey.ShouldBe(expectedKey);
    }
}
