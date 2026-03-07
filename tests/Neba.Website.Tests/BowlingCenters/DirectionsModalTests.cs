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
    private readonly BunitJSObjectInterop _modalModuleInterop;

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
        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, false)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, new DirectionsState())
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        cut.FindAll(".neba-modal-backdrop").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should render location input UI in DirectionsPreview mode")]
    public void Render_ShouldRenderDirectionsPreviewContent_WhenModeIsPreview()
    {
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        cut.Find(".neba-space-y-4").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should show Use My Current Location button in preview mode")]
    public void Render_ShouldShowUseCurrentLocationButton_WhenInPreviewMode()
    {
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        cut.Markup.ShouldContain("Use My Current Location");
    }

    [Fact(DisplayName = "Should show address input in preview mode")]
    public void Render_ShouldShowManualAddressInput_WhenInPreviewMode()
    {
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        cut.Find("input#address-input").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should show selected center name in modal title")]
    public void Render_ShouldShowCenterNameInTitle_WhenStateHasSelectedCenterName()
    {
        var state = new DirectionsState
        {
            Mode = MapMode.DirectionsPreview,
            SelectedCenterName = "Spare Time Lanes"
        };

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        cut.Markup.ShouldContain("Directions to Spare Time Lanes");
    }

    [Fact(DisplayName = "Should show route summary when mode is DirectionsActive")]
    public void Render_ShouldShowRouteSummary_WhenModeIsDirectionsActive()
    {
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

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        cut.Markup.ShouldContain("Open in Maps App");
    }

    [Fact(DisplayName = "Should show error message when state has an error")]
    public void Render_ShouldShowErrorMessage_WhenStateHasError()
    {
        var state = new DirectionsState
        {
            Mode = MapMode.DirectionsPreview,
            ErrorMessage = "Location access denied."
        };

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        cut.Markup.ShouldContain("Location access denied.");
    }

    [Fact(DisplayName = "Should invoke OnClose when Cancel button is clicked")]
    public async Task HandleClose_ShouldInvokeOnClose_WhenCancelButtonClicked()
    {
        var closeCalled = false;
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closeCalled = true))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        var cancelButton = cut.FindAll(".neba-btn-secondary").First(b => b.TextContent.Trim() == "Cancel");
        await cut.InvokeAsync(() => cancelButton.Click());

        closeCalled.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should render route mini-map container when DirectionsActive and RouteGeoJson is set")]
    public void Render_ShouldShowRouteMapContainer_WhenDirectionsActiveWithGeoJson()
    {
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

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        cut.Find("#directions-mini-map").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should not render route mini-map container when RouteGeoJson is null")]
    public void Render_ShouldNotShowRouteMapContainer_WhenRouteGeoJsonIsNull()
    {
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

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        cut.FindAll("#directions-mini-map").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should call initializeRouteMap JS function when DirectionsActive with RouteGeoJson")]
    public void OnAfterRender_ShouldCallInitializeRouteMap_WhenDirectionsActiveWithGeoJson()
    {
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

        _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        _modalModuleInterop.VerifyInvoke("initializeRouteMap", 1);
    }

    [Fact(DisplayName = "Should call disposeRouteMap JS function when Close button is clicked")]
    public async Task HandleClose_ShouldCallDisposeRouteMap_WhenClosed()
    {
        var state = new DirectionsState { Mode = MapMode.DirectionsPreview };

        var cut = _ctx.Render<DirectionsModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.State, state)
            .Add(x => x.OnLocationSelected, EventCallback.Factory.Create<double[]>(this, _ => { })));

        var cancelButton = cut.FindAll(".neba-btn-secondary").First(b => b.TextContent.Trim() == "Cancel");
        await cut.InvokeAsync(() => cancelButton.Click());

        _modalModuleInterop.VerifyInvoke("disposeRouteMap", 1);
    }
}
