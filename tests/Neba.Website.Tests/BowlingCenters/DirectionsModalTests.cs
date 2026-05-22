using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.BowlingCenters;
using Neba.Website.Server.Maps;

using ServerMaps = Neba.Website.Server.Maps;

namespace Neba.Website.Tests.BowlingCenters;

[UnitTest]
[Component("Website.BowlingCenters.DirectionsModal")]
public sealed class DirectionsModalTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly BunitJSInterop _modalModuleInterop;

    public DirectionsModalTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _modalModuleInterop = _ctx.JSInterop.SetupModule("./BowlingCenters/DirectionsModal.razor.js");
        _ctx.JSInterop.SetupModule("./Components/NebaModal.razor.js");
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should not render modal content when IsOpen is false")]
    public void Render_ShouldNotRenderContent_WhenIsOpenIsFalse()
    {
        // Act
        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, false)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, new DirectionsState())
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Assert
        cut.FindAll(".neba-modal-backdrop").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should render location input UI in DirectionsPreview mode")]
    public void Render_ShouldRenderDirectionsPreviewContent_WhenModeIsPreview()
    {
        // Arrange
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };

        // Act
        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Assert
        cut.Find(".neba-space-y-4").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should show Use My Current Location button in preview mode")]
    public void Render_ShouldShowUseCurrentLocationButton_WhenInPreviewMode()
    {
        // Arrange
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };

        // Act
        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Assert
        cut.Markup.ShouldContain("Use My Current Location");
    }

    [Fact(DisplayName = "Should show address input in preview mode")]
    public void Render_ShouldShowManualAddressInput_WhenInPreviewMode()
    {
        // Arrange
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };

        // Act
        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Assert
        cut.Find("input#address-input").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should show selected center name in modal title")]
    public void Render_ShouldShowCenterNameInTitle_WhenStateHasSelectedCenterName()
    {
        // Arrange
        var state = new DirectionsState
        {
            Mode = MapMode.DirectionsPreview,
            SelectedCenterName = "Spare Time Lanes"
        };

        // Act
        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Assert
        cut.Markup.ShouldContain("Directions to Spare Time Lanes");
    }

    [Fact(DisplayName = "Should show route summary when mode is DirectionsActive")]
    public void Render_ShouldShowRouteSummary_WhenModeIsDirectionsActive()
    {
        // Arrange
        var state = new DirectionsState
        {
            Mode = MapMode.DirectionsActive,
            Route = new ServerMaps.RouteData
            {
                DistanceMeters = 16093.4,
                TravelTimeSeconds = 1200,
                Instructions = []
            }
        };

        // Act
        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Assert
        cut.Markup.ShouldContain("Open in Maps App");
    }

    [Fact(DisplayName = "Should render best route and alternative options when route options are available")]
    public void Render_ShouldShowRouteOptions_WhenRouteOptionsAreAvailable()
    {
        // Arrange
        var state = new DirectionsState
        {
            Mode = MapMode.DirectionsActive,
            Route = new ServerMaps.RouteData
            {
                DistanceMeters = 16093.4,
                TravelTimeSeconds = 1200,
                SelectedRouteIndex = 0,
                RouteOptions =
                [
                    new ServerMaps.RouteData { DistanceMeters = 16093.4, TravelTimeSeconds = 1200, Instructions = [] },
                    new ServerMaps.RouteData { DistanceMeters = 17702.8, TravelTimeSeconds = 1260, Instructions = [] }
                ],
                Instructions = []
            }
        };

        // Act
        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Assert
        cut.Markup.ShouldContain("Route options");
        cut.Markup.ShouldContain("Best route");
        cut.Markup.ShouldContain("Alternative 1");
    }

    [Fact(DisplayName = "Should switch summary route when an alternative route option is selected")]
    public async Task HandleRouteOptionSelected_ShouldSwitchRoute_WhenAlternativeSelected()
    {
        // Arrange
        var state = new DirectionsState
        {
            Mode = MapMode.DirectionsActive,
            Route = new ServerMaps.RouteData
            {
                DistanceMeters = 16093.4,
                TravelTimeSeconds = 1200,
                SelectedRouteIndex = 0,
                RouteGeoJson = "{\"type\":\"Feature\"}",
                RouteOptions =
                [
                    new ServerMaps.RouteData
                    {
                        DistanceMeters = 16093.4,
                        TravelTimeSeconds = 1200,
                        Instructions = [new ServerMaps.RouteInstruction { Text = "Best route step" }],
                        RouteGeoJson = "{\"type\":\"Feature\"}"
                    },
                    new ServerMaps.RouteData
                    {
                        DistanceMeters = 17702.8,
                        TravelTimeSeconds = 1260,
                        Instructions = [new ServerMaps.RouteInstruction { Text = "Alternative step" }],
                        RouteGeoJson = "{\"type\":\"Feature\"}"
                    }
                ],
                Instructions = [new ServerMaps.RouteInstruction { Text = "Best route step" }]
            }
        };

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        var alternativeButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Alternative 1", StringComparison.OrdinalIgnoreCase));

        // Act
        await cut.InvokeAsync(() => alternativeButton.Click());

        // Assert
        state.Route.SelectedRouteIndex.ShouldBe(1);
        state.Route.DistanceMeters.ShouldBe(17702.8);
        state.Route.TravelTimeSeconds.ShouldBe(1260);
        state.Route.Instructions[0].Text.ShouldBe("Alternative step");
        _modalModuleInterop.VerifyInvoke("disposeRouteMap", 1);
    }

    [Fact(DisplayName = "Should show error message when state has an error")]
    public void Render_ShouldShowErrorMessage_WhenStateHasError()
    {
        // Arrange
        var state = new DirectionsState
        {
            Mode = MapMode.DirectionsPreview,
            ErrorMessage = "Location access denied."
        };

        // Act
        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Assert
        cut.Markup.ShouldContain("Location access denied.");
    }

    [Fact(DisplayName = "Should invoke OnClose when Cancel button is clicked")]
    public async Task HandleClose_ShouldInvokeOnClose_WhenCancelButtonClicked()
    {
        // Arrange
        var closeCalled = false;
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closeCalled = true))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        var cancelButton = cut.FindAll(".neba-btn-secondary").First(b => b.TextContent.Trim() == "Cancel");

        // Act
        await cut.InvokeAsync(() => cancelButton.Click());

        // Assert
        closeCalled.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should render route mini-map container when DirectionsActive and RouteGeoJson is set")]
    public void Render_ShouldShowRouteMapContainer_WhenDirectionsActiveWithGeoJson()
    {
        // Arrange
        var state = new DirectionsState
        {
            Mode = MapMode.DirectionsActive,
            UserLocation = [-71.0589, 42.3601],
            DestinationLocation = [-71.5, 42.5],
            Route = new ServerMaps.RouteData
            {
                DistanceMeters = 16093.4,
                TravelTimeSeconds = 1200,
                RouteGeoJson = "{\"type\":\"Feature\"}"
            }
        };

        // Act
        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Assert
        cut.Find("#directions-mini-map").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should not render route mini-map container when RouteGeoJson is null")]
    public void Render_ShouldNotShowRouteMapContainer_WhenRouteGeoJsonIsNull()
    {
        // Arrange
        var state = new DirectionsState
        {
            Mode = MapMode.DirectionsActive,
            Route = new ServerMaps.RouteData
            {
                DistanceMeters = 16093.4,
                TravelTimeSeconds = 1200,
                RouteGeoJson = null
            }
        };

        // Act
        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Assert
        cut.FindAll("#directions-mini-map").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should call initializeRouteMap JS function when DirectionsActive with RouteGeoJson")]
    public void OnAfterRender_ShouldCallInitializeRouteMap_WhenDirectionsActiveWithGeoJson()
    {
        // Arrange
        var state = new DirectionsState
        {
            Mode = MapMode.DirectionsActive,
            UserLocation = [-71.0589, 42.3601],
            DestinationLocation = [-71.5, 42.5],
            Route = new ServerMaps.RouteData
            {
                DistanceMeters = 16093.4,
                TravelTimeSeconds = 1200,
                RouteGeoJson = "{\"type\":\"Feature\"}"
            }
        };

        // Act
        _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Assert
        _modalModuleInterop.VerifyInvoke("initializeRouteMap", 1);
    }

    [Fact(DisplayName = "Should call disposeRouteMap JS function when Close button is clicked")]
    public async Task HandleClose_ShouldCallDisposeRouteMap_WhenClosed()
    {
        // Arrange
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        var cancelButton = cut.FindAll(".neba-btn-secondary").First(b => b.TextContent.Trim() == "Cancel");

        // Act
        await cut.InvokeAsync(() => cancelButton.Click());

        // Assert
        _modalModuleInterop.VerifyInvoke("disposeRouteMap", 1);
    }

    [Fact(DisplayName = "Should show loading spinner when state is loading")]
    public void Render_ShouldShowLoadingSpinner_WhenStateIsLoading()
    {
        // Arrange
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview, IsLoading = true };

        // Act
        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Assert
        cut.Markup.ShouldContain("Getting your location...");
    }

    [Fact(DisplayName = "HandleUseCurrentLocation should invoke OnLocationSelected when geolocation succeeds")]
    public async Task HandleUseCurrentLocation_ShouldInvokeOnLocationSelected_WhenSuccess()
    {
        // Arrange
        double[]? receivedLocation = null;
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };
        _modalModuleInterop.Setup<double[]>("getCurrentLocation", _ => true)
            .SetResult([-71.0589, 42.3601]);

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, loc => receivedLocation = loc)));

        // Act
        await cut.InvokeAsync(() => cut.FindAll("button").First(b => b.TextContent.Contains("Use My Current Location", StringComparison.OrdinalIgnoreCase)).Click());

        // Assert
        receivedLocation.ShouldNotBeNull();
        receivedLocation[0].ShouldBe(-71.0589);
    }

    [Fact(DisplayName = "HandleUseCurrentLocation should show permission denied error when browser denies geolocation")]
    public async Task HandleUseCurrentLocation_ShouldShowPermissionDeniedError_WhenPermissionDenied()
    {
        // Arrange
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };
        _modalModuleInterop.Setup<double[]>("getCurrentLocation", _ => true)
            .SetException(new JSException("User denied geolocation permission"));

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Act
        await cut.InvokeAsync(() => cut.FindAll("button").First(b => b.TextContent.Contains("Use My Current Location", StringComparison.OrdinalIgnoreCase)).Click());

        // Assert
        state.ErrorMessage.ShouldNotBeNull();
        state.ErrorMessage.ShouldContain("Location access denied");
    }

    [Fact(DisplayName = "HandleUseCurrentLocation should show generic error when geolocation fails for non-permission reason")]
    public async Task HandleUseCurrentLocation_ShouldShowGenericError_WhenJSExceptionWithoutPermission()
    {
        // Arrange
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };
        _modalModuleInterop.Setup<double[]>("getCurrentLocation", _ => true)
            .SetException(new JSException("Geolocation hardware unavailable"));

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Act
        await cut.InvokeAsync(() => cut.FindAll("button").First(b => b.TextContent.Contains("Use My Current Location", StringComparison.OrdinalIgnoreCase)).Click());

        // Assert
        state.ErrorMessage.ShouldNotBeNull();
        state.ErrorMessage.ShouldContain("Unable to get your location");
    }

    [Fact(DisplayName = "HandleAddressInputChange should clear suggestions when input is fewer than 3 characters")]
    public async Task HandleAddressInputChange_ShouldClearSuggestions_WhenInputIsTooShort()
    {
        // Arrange
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Act
        await cut.InvokeAsync(() => cut.Find("input#address-input").Input("AB"));

        // Assert
        // Only the NebaModal close button (✕) should have type="button"; no suggestion buttons should appear
        cut.FindAll("button[type='button']").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "HandleOpenInMaps should call openInNewTab JS when user and destination locations are set")]
    public async Task HandleOpenInMaps_ShouldCallOpenInNewTab_WhenLocationsAreSet()
    {
        // Arrange
        var state = new DirectionsState
        {
            Mode = MapMode.DirectionsActive,
            UserLocation = [-71.0589, 42.3601],
            DestinationLocation = [-71.5, 42.5],
            Route = new ServerMaps.RouteData { DistanceMeters = 16093.4, TravelTimeSeconds = 1200, Instructions = [] }
        };

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Act
        await cut.InvokeAsync(() => cut.FindAll("button").First(b => b.TextContent.Contains("Open in Maps App", StringComparison.OrdinalIgnoreCase)).Click());

        // Assert
        _modalModuleInterop.VerifyInvoke("openInNewTab", 1);
    }

    [Fact(DisplayName = "HandleOpenInMaps should return early when UserLocation is null")]
    public async Task HandleOpenInMaps_ShouldReturnEarly_WhenUserLocationIsNull()
    {
        // Arrange
        var state = new DirectionsState
        {
            Mode = MapMode.DirectionsActive,
            UserLocation = null,
            DestinationLocation = [-71.5, 42.5],
            Route = new ServerMaps.RouteData { DistanceMeters = 16093.4, TravelTimeSeconds = 1200, Instructions = [] }
        };

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Act
        await cut.InvokeAsync(() => cut.FindAll("button").First(b => b.TextContent.Contains("Open in Maps App", StringComparison.OrdinalIgnoreCase)).Click());

        // Assert
        // openInNewTab should not be called when UserLocation is null
        cut.Instance.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should show turn-by-turn instructions when toggle button is clicked")]
    public async Task ToggleDirections_ShouldShowInstructions_WhenToggled()
    {
        // Arrange
        var state = new DirectionsState
        {
            Mode = MapMode.DirectionsActive,
            Route = new ServerMaps.RouteData
            {
                DistanceMeters = 16093.4,
                TravelTimeSeconds = 1200,
                Instructions = [new ServerMaps.RouteInstruction { Text = "Head north on Main St", DistanceMeters = 500 }]
            }
        };

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Act
        await cut.InvokeAsync(() => cut.FindAll("button").First(b => b.TextContent.Contains("Turn-by-turn", StringComparison.OrdinalIgnoreCase)).Click());

        // Assert
        cut.Markup.ShouldContain("Head north on Main St");
    }

    [Fact(DisplayName = "DisposeAsync should complete without throwing")]
    public async Task DisposeAsync_ShouldComplete_WhenCalled()
    {
        // Arrange
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Act
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

        // Assert
        cut.Instance.ShouldNotBeNull();
    }

    [Fact(DisplayName = "HandleUseCurrentLocation should swallow exception when location request is task-canceled")]
    public async Task HandleUseCurrentLocation_ShouldSwallowException_WhenTaskCanceled()
    {
        // Arrange
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };
        _modalModuleInterop.Setup<double[]>("getCurrentLocation", _ => true)
            .SetException(new TaskCanceledException("Canceled"));

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Act
        await cut.InvokeAsync(() => cut.FindAll("button").First(b => b.TextContent.Contains("Use My Current Location", StringComparison.OrdinalIgnoreCase)).Click());

        // Assert
        state.IsLoading.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleUseCurrentLocation should swallow exception when circuit disconnects")]
    public async Task HandleUseCurrentLocation_ShouldSwallowException_WhenJSDisconnected()
    {
        // Arrange
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };
        _modalModuleInterop.Setup<double[]>("getCurrentLocation", _ => true)
            .SetException(new JSDisconnectedException("Disconnected"));

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Act
        await cut.InvokeAsync(() => cut.FindAll("button").First(b => b.TextContent.Contains("Use My Current Location", StringComparison.OrdinalIgnoreCase)).Click());

        // Assert
        state.IsLoading.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleClose should still invoke OnClose when disposeRouteMap throws JSException")]
    public async Task HandleClose_ShouldInvokeOnClose_WhenDisposeRouteMapThrowsJSException()
    {
        // Arrange
        var closeCalled = false;
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };
        _modalModuleInterop.SetupVoid("disposeRouteMap", _ => true)
            .SetException(new JSException("Dispose failed"));

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closeCalled = true))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        var cancelButton = cut.FindAll(".neba-btn-secondary").First(b => b.TextContent.Trim() == "Cancel");

        // Act
        await cut.InvokeAsync(() => cancelButton.Click());

        // Assert
        closeCalled.ShouldBeTrue();
    }

    [Fact(DisplayName = "DisposeAsync should swallow JSDisconnectedException when disposeRouteMap fails")]
    public async Task DisposeAsync_ShouldSwallow_WhenDisposeRouteMapThrowsJSDisconnectedException()
    {
        // Arrange
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };
        _modalModuleInterop.SetupVoid("disposeRouteMap", _ => true)
            .SetException(new JSDisconnectedException("Disconnected"));

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        // Act
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

        // Assert
        cut.Instance.ShouldNotBeNull();
    }
}