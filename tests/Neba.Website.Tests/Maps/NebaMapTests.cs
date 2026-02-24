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
    public void Render_ContainerDiv_HasNebaMapIdPrefix()
    {
        var cut = _ctx.Render<NebaMap>();

        cut.Find("div[id^='neba-map-']").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should apply default height and width to container style")]
    public void Render_DefaultDimensions_AppliedToContainerStyle()
    {
        var cut = _ctx.Render<NebaMap>();

        var style = cut.Find("div").GetAttribute("style");
        style.ShouldNotBeNull();
        style.ShouldContain("height: 600px");
        style.ShouldContain("width: 100%");
    }

    [Fact(DisplayName = "Should apply custom height and width to container style")]
    public void Render_CustomDimensions_AppliedToContainerStyle()
    {
        var cut = _ctx.Render<NebaMap>(parameters =>
            parameters.Add(p => p.Height, "400px")
                      .Add(p => p.Width, "50%"));

        var style = cut.Find("div").GetAttribute("style");
        style.ShouldNotBeNull();
        style.ShouldContain("height: 400px");
        style.ShouldContain("width: 50%");
    }

    [Fact(DisplayName = "Should apply custom CSS class to container div")]
    public void Render_CustomCssClass_AppliedToContainer()
    {
        var cut = _ctx.Render<NebaMap>(parameters =>
            parameters.Add(p => p.CssClass, "my-map-class"));

        cut.Find("div.my-map-class").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should call initializeMap JS function on first render")]
    public void OnAfterRender_FirstRender_CallsInitializeMap()
    {
        _ctx.Render<NebaMap>();

        _moduleInterop.VerifyInvoke("initializeMap");
    }

    [Fact(DisplayName = "Should call initializeMap exactly once across multiple renders")]
    public void OnAfterRender_SubsequentRender_DoesNotCallInitializeMapAgain()
    {
        var cut = _ctx.Render<NebaMap>();
        cut.Render(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(NebaMap.Zoom), 10 }
        }));

        _moduleInterop.VerifyInvoke("initializeMap", 1);
    }

    [Fact(DisplayName = "Should call updateMarkers when locations change after initialization")]
    public void OnParametersSet_AfterInit_CallsUpdateMarkers()
    {
        var cut = _ctx.Render<NebaMap>();
        var locations = new[]
        {
            new NebaMapLocation("loc-1", "Test Bowl", "123 Main St", 42.3601, -71.0589)
        };

        cut.Render(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(NebaMap.Locations), locations }
        }));

        _moduleInterop.VerifyInvoke("updateMarkers");
    }

    [Fact(DisplayName = "Should invoke OnMapReady callback when NotifyMapReady is called")]
    public async Task NotifyMapReady_InvokesOnMapReadyCallback()
    {
        var mapReadyCalled = false;
        var cut = _ctx.Render<NebaMap>(parameters =>
            parameters.Add(p => p.OnMapReady, EventCallback.Factory.Create(this, () => mapReadyCalled = true)));

        await cut.InvokeAsync(() => cut.Instance.NotifyMapReady());

        mapReadyCalled.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should invoke OnBoundsChanged callback with correct bounds")]
    public async Task NotifyBoundsChanged_InvokesOnBoundsChangedCallback_WithCorrectBounds()
    {
        MapBounds? receivedBounds = null;
        var expectedBounds = new MapBounds(North: 43.0, South: 42.0, East: -70.0, West: -72.0);
        var cut = _ctx.Render<NebaMap>(parameters =>
            parameters.Add(p => p.OnBoundsChanged, EventCallback.Factory.Create<MapBounds>(this, b => receivedBounds = b)));

        await cut.InvokeAsync(() => cut.Instance.NotifyBoundsChanged(expectedBounds));

        receivedBounds.ShouldNotBeNull();
        receivedBounds.ShouldBe(expectedBounds);
    }

    [Fact(DisplayName = "Should call focusOnLocation JS function with location id")]
    public async Task FocusOnLocationAsync_CallsJsWithLocationId()
    {
        var cut = _ctx.Render<NebaMap>();

        await cut.InvokeAsync(() => cut.Instance.FocusOnLocationAsync("loc-1"));

        _moduleInterop.VerifyInvoke("focusOnLocation", 1);
    }

    [Fact(DisplayName = "Should call fitBounds JS function")]
    public async Task FitBoundsAsync_CallsJs()
    {
        var cut = _ctx.Render<NebaMap>();

        await cut.InvokeAsync(() => cut.Instance.FitBoundsAsync());

        _moduleInterop.VerifyInvoke("fitBounds", 1);
    }

    [Fact(DisplayName = "Should call closePopup JS function")]
    public async Task ClosePopupAsync_CallsJs()
    {
        var cut = _ctx.Render<NebaMap>();

        await cut.InvokeAsync(() => cut.Instance.ClosePopupAsync());

        _moduleInterop.VerifyInvoke("closePopup", 1);
    }

    [Fact(DisplayName = "Should call setMapStyle JS function with requested style")]
    public async Task SetMapStyleAsync_CallsJsWithStyle()
    {
        var cut = _ctx.Render<NebaMap>();

        await cut.InvokeAsync(() => cut.Instance.SetMapStyleAsync("satellite"));

        _moduleInterop.VerifyInvoke("setMapStyle", 1);
    }

    [Fact(DisplayName = "Should call enterDirectionsPreview JS function with location id")]
    public async Task EnterDirectionsPreviewAsync_CallsJsWithLocationId()
    {
        var cut = _ctx.Render<NebaMap>();

        await cut.InvokeAsync(() => cut.Instance.EnterDirectionsPreviewAsync("loc-2"));

        _moduleInterop.VerifyInvoke("enterDirectionsPreview", 1);
    }

    [Fact(DisplayName = "Should call exitDirectionsMode JS function")]
    public async Task ExitDirectionsModeAsync_CallsJs()
    {
        var cut = _ctx.Render<NebaMap>();

        await cut.InvokeAsync(() => cut.Instance.ExitDirectionsModeAsync());

        _moduleInterop.VerifyInvoke("exitDirectionsMode", 1);
    }

    [Fact(DisplayName = "Should return route data returned by showRoute JS function")]
    public async Task ShowRouteAsync_WhenModuleLoaded_ReturnsRouteData()
    {
        var expectedRoute = new RouteData { DistanceMeters = 16093.4, TravelTimeSeconds = 1200 };
        _moduleInterop.Setup<RouteData>("showRoute", _ => true).SetResult(expectedRoute);

        var cut = _ctx.Render<NebaMap>();
        var origin = new[] { -71.0589, 42.3601 };
        var destination = new[] { -71.5, 42.5 };

        var result = await cut.InvokeAsync(() => cut.Instance.ShowRouteAsync(origin, destination));

        result.ShouldNotBeNull();
        result.DistanceMeters.ShouldBe(16093.4);
        result.TravelTimeSeconds.ShouldBe(1200);
    }

    [Fact(DisplayName = "Should call dispose JS function when component is disposed")]
    public async Task DisposeAsync_CallsJsDispose()
    {
        var cut = _ctx.Render<NebaMap>();

        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

        _moduleInterop.VerifyInvoke("dispose", 1);
    }
}
