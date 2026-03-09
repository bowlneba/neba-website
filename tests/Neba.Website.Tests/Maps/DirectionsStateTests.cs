using Neba.TestFactory.Attributes;
using Neba.Website.Server.Maps;

using RouteData = Neba.Website.Server.Maps.RouteData;

namespace Neba.Website.Tests.Maps;

[UnitTest]
[Component("Website.Maps.DirectionsState")]
public sealed class DirectionsStateTests
{
    [Fact(DisplayName = "Default state has Overview mode")]
    public void DefaultState_ShouldHaveOverviewMode()
    {
        var state = new DirectionsState();

        state.Mode.ShouldBe(MapMode.Overview);
    }

    [Fact(DisplayName = "Default state IsLoading is false")]
    public void DefaultState_ShouldNotBeLoading()
    {
        var state = new DirectionsState();

        state.IsLoading.ShouldBeFalse();
    }

    [Fact(DisplayName = "Default state all nullable properties are null")]
    public void DefaultState_ShouldHaveNullNullableProperties()
    {
        var state = new DirectionsState();

        state.SelectedCenterId.ShouldBeNull();
        state.SelectedCenterName.ShouldBeNull();
        state.UserLocation.ShouldBeNull();
        state.UserAddress.ShouldBeNull();
        state.DestinationLocation.ShouldBeNull();
        state.Route.ShouldBeNull();
        state.ErrorMessage.ShouldBeNull();
    }

    [Fact(DisplayName = "Reset restores all properties to defaults after being set")]
    public void Reset_ShouldRestoreAllPropertiesToDefaults_AfterBeingSet()
    {
        var state = new DirectionsState
        {
            Mode = MapMode.DirectionsActive,
            SelectedCenterId = "center-1",
            SelectedCenterName = "Spare Time Lanes",
            UserLocation = [-71.0589, 42.3601],
            UserAddress = "123 Main St, Boston, MA",
            DestinationLocation = [-71.5, 42.5],
            Route = new RouteData { DistanceMeters = 1000, TravelTimeSeconds = 600 },
            IsLoading = true,
            ErrorMessage = "Something went wrong"
        };

        state.Reset();

        state.Mode.ShouldBe(MapMode.Overview);
        state.SelectedCenterId.ShouldBeNull();
        state.SelectedCenterName.ShouldBeNull();
        state.UserLocation.ShouldBeNull();
        state.UserAddress.ShouldBeNull();
        state.DestinationLocation.ShouldBeNull();
        state.Route.ShouldBeNull();
        state.IsLoading.ShouldBeFalse();
        state.ErrorMessage.ShouldBeNull();
    }

    [Fact(DisplayName = "Reset is idempotent when called on a default state")]
    public void Reset_ShouldBeIdempotent_WhenCalledOnDefaultState()
    {
        var state = new DirectionsState();

        state.Reset();

        state.Mode.ShouldBe(MapMode.Overview);
        state.IsLoading.ShouldBeFalse();
        state.ErrorMessage.ShouldBeNull();
    }
}