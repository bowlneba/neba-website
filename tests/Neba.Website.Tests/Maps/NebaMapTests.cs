using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Maps;

using RouteData = Neba.Website.Server.Maps.RouteData;

namespace Neba.Website.Tests.Maps;

[UnitTest]
[Component("Website.Maps.NebaMap")]
public sealed class NebaMapTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly BunitJSInterop _moduleInterop;

    public NebaMapTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _moduleInterop = _ctx.JSInterop.SetupModule("./Maps/NebaMap.razor.js");

        _ctx.Services.AddSingleton(new AzureMapsSettings
        {
            AccountId = "test-account",
            SubscriptionKey = "test-subscription-key"
        });
        _ctx.Services.AddSingleton<ILogger<NebaMap>>(NullLogger<NebaMap>.Instance);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render map container div with neba-map id prefix")]
    public void Render_ShouldRenderContainerDiv_WithNebaMapIdPrefix()
    {
        // Act
        var cut = _ctx.Render<NebaMap>();

        // Assert
        cut.Find("div[id^='neba-map-']").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should apply default height and width to container style")]
    public void Render_ShouldApplyDefaultDimensions_WhenRendered()
    {
        // Act
        var cut = _ctx.Render<NebaMap>();

        // Assert
        var style = cut.Find("div").GetAttribute("style");
        style.ShouldNotBeNull();
        style.ShouldContain("height: 600px");
        style.ShouldContain("width: 100%");
    }

    [Fact(DisplayName = "Should apply custom height and width to container style")]
    public void Render_ShouldApplyCustomDimensions_WhenCustomDimensionsProvided()
    {
        // Act
        var cut = _ctx.Render<NebaMap>(parameters =>
            parameters.Add(p => p.Height, "400px")
                      .Add(p => p.Width, "50%"));

        // Assert
        var style = cut.Find("div").GetAttribute("style");
        style.ShouldNotBeNull();
        style.ShouldContain("height: 400px");
        style.ShouldContain("width: 50%");
    }

    [Fact(DisplayName = "Should apply custom CSS class to container div")]
    public void Render_ShouldApplyCssClass_WhenCustomClassProvided()
    {
        // Act
        var cut = _ctx.Render<NebaMap>(parameters =>
            parameters.Add(p => p.CssClass, "my-map-class"));

        // Assert
        cut.Find("div.my-map-class").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should call initializeMap JS function on first render")]
    public void OnAfterRender_ShouldCallInitializeMap_WhenFirstRender()
    {
        // Act
        _ctx.Render<NebaMap>();

        // Assert
        _moduleInterop.VerifyInvoke("initializeMap");
    }

    [Fact(DisplayName = "Should call initializeMap exactly once across multiple renders")]
    public void OnAfterRender_ShouldNotCallInitializeMapAgain_WhenSubsequentRender()
    {
        // Arrange
        var cut = _ctx.Render<NebaMap>();

        // Act
        cut.Render(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(NebaMap.Zoom), 10 }
        }));

        // Assert
        _moduleInterop.VerifyInvoke("initializeMap", 1);
    }

    [Fact(DisplayName = "Should call updateMarkers when locations change after initialization")]
    public void OnParametersSet_ShouldCallUpdateMarkers_WhenLocationsChangeAfterInit()
    {
        // Arrange
        var cut = _ctx.Render<NebaMap>();
        var locations = new[]
        {
            new NebaMapLocation("loc-1", "Test Bowl", "123 Main St", 42.3601, -71.0589)
        };

        // Act
        cut.Render(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(NebaMap.Locations), locations }
        }));

        // Assert
        _moduleInterop.VerifyInvoke("updateMarkers");
    }

    [Fact(DisplayName = "Should invoke OnMapReady callback when NotifyMapReady is called")]
    public async Task NotifyMapReady_ShouldInvokeOnMapReadyCallback_WhenCalled()
    {
        // Arrange
        var mapReadyCalled = false;
        var cut = _ctx.Render<NebaMap>(parameters =>
            parameters.Add(p => p.OnMapReady, EventCallback.Factory.Create(this, () => mapReadyCalled = true)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.NotifyMapReady());

        // Assert
        mapReadyCalled.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should invoke OnBoundsChanged callback with correct bounds")]
    public async Task NotifyBoundsChanged_ShouldInvokeCallback_WhenCalled()
    {
        // Arrange
        MapBounds? receivedBounds = null;
        var expectedBounds = new MapBounds(North: 43.0, South: 42.0, East: -70.0, West: -72.0);
        var cut = _ctx.Render<NebaMap>(parameters =>
            parameters.Add(p => p.OnBoundsChanged, EventCallback.Factory.Create<MapBounds>(this, b => receivedBounds = b)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.NotifyBoundsChanged(expectedBounds));

        // Assert
        receivedBounds.ShouldNotBeNull();
        receivedBounds.ShouldBe(expectedBounds);
    }

    [Fact(DisplayName = "Should call focusOnLocation JS function when location id is provided")]
    public async Task FocusOnLocationAsync_ShouldCallFocusOnLocationJs_WhenLocationIdProvided()
    {
        // Arrange
        var cut = _ctx.Render<NebaMap>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.FocusOnLocationAsync("loc-1"));

        // Assert
        _moduleInterop.VerifyInvoke("focusOnLocation", 1);
    }

    [Fact(DisplayName = "Should call fitBounds JS function when invoked")]
    public async Task FitBoundsAsync_ShouldCallFitBoundsJs_WhenCalled()
    {
        // Arrange
        var cut = _ctx.Render<NebaMap>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.FitBoundsAsync());

        // Assert
        _moduleInterop.VerifyInvoke("fitBounds", 1);
    }

    [Fact(DisplayName = "Should call closePopup JS function when invoked")]
    public async Task ClosePopupAsync_ShouldCallClosePopupJs_WhenCalled()
    {
        // Arrange
        var cut = _ctx.Render<NebaMap>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.ClosePopupAsync());

        // Assert
        _moduleInterop.VerifyInvoke("closePopup", 1);
    }

    [Fact(DisplayName = "Should call setMapStyle JS function when style is provided")]
    public async Task SetMapStyleAsync_ShouldCallSetMapStyleJs_WhenStyleProvided()
    {
        // Arrange
        var cut = _ctx.Render<NebaMap>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.SetMapStyleAsync("satellite"));

        // Assert
        _moduleInterop.VerifyInvoke("setMapStyle", 1);
    }

    [Fact(DisplayName = "Should call enterDirectionsPreview JS function when location id is provided")]
    public async Task EnterDirectionsPreviewAsync_ShouldCallJs_WhenLocationIdProvided()
    {
        // Arrange
        var cut = _ctx.Render<NebaMap>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.EnterDirectionsPreviewAsync("loc-2"));

        // Assert
        _moduleInterop.VerifyInvoke("enterDirectionsPreview", 1);
    }

    [Fact(DisplayName = "Should call exitDirectionsMode JS function when invoked")]
    public async Task ExitDirectionsModeAsync_ShouldCallExitDirectionsModeJs_WhenCalled()
    {
        // Arrange
        var cut = _ctx.Render<NebaMap>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.ExitDirectionsModeAsync());

        // Assert
        _moduleInterop.VerifyInvoke("exitDirectionsMode", 1);
    }

    [Fact(DisplayName = "Should return route data when module is loaded")]
    public async Task ShowRouteAsync_ShouldReturnRouteData_WhenModuleIsLoaded()
    {
        // Arrange
        var expectedRoute = new RouteData { DistanceMeters = 16093.4, TravelTimeSeconds = 1200 };
        _moduleInterop.Setup<RouteData>("showRoute", _ => true).SetResult(expectedRoute);

        var cut = _ctx.Render<NebaMap>();
        var origin = new[] { -71.0589, 42.3601 };
        var destination = new[] { -71.5, 42.5 };

        // Act
        var result = await cut.InvokeAsync(() => cut.Instance.ShowRouteAsync(origin, destination));

        // Assert
        result.ShouldNotBeNull();
        result.DistanceMeters.ShouldBe(16093.4);
        result.TravelTimeSeconds.ShouldBe(1200);
    }

    [Fact(DisplayName = "Should call dispose JS function when component is disposed")]
    public async Task DisposeAsync_ShouldCallDisposeJs_WhenDisposed()
    {
        // Arrange
        var cut = _ctx.Render<NebaMap>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

        // Assert
        _moduleInterop.VerifyInvoke("dispose", 1);
    }
}
