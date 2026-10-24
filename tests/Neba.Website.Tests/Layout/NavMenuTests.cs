using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.JSInterop;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Layout;

namespace Neba.Website.Tests.Layout;

[UnitTest]
[Component("Website.Layout.NavMenu")]
public sealed class NavMenuTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly BunitJSModuleInterop _navMenuModule;

    public NavMenuTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _navMenuModule = _ctx.JSInterop.SetupModule("./Layout/NavMenu.razor.js");
        _ctx.AddAuthorization();

        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>(MockBehavior.Strict);
        mockWebHostEnvironment.Setup(x => x.EnvironmentName).Returns("Development");

        _ctx.Services.AddSingleton(mockWebHostEnvironment.Object);
        _ctx.Services.AddSingleton<ILogger<NavMenu>>(NullLogger<NavMenu>.Instance);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render nav element when component is rendered")]
    public void Render_ShouldRenderNavElement_WhenRendered()
    {
        // Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Find("nav.neba-navbar").ShouldNotBeNull();
    }

    [Fact(DisplayName = "ToggleNavMenu should expand the menu when it is collapsed")]
    public async Task ToggleNavMenu_ShouldExpandMenu_WhenMenuIsCollapsed()
    {
        // Arrange
        var cut = _ctx.Render<NavMenu>();

        // Act
        await cut.InvokeAsync(() => cut.Find("button.neba-menu-toggle").Click());

        // Assert
        cut.Find("ul.neba-nav-menu.active").ShouldNotBeNull();
        cut.Find("nav.neba-navbar.menu-open").ShouldNotBeNull();
    }

    [Fact(DisplayName = "CloseMenu should collapse the navigation menu when called")]
    public async Task CloseMenu_ShouldCollapseMenu_WhenCalled()
    {
        // Arrange
        var cut = _ctx.Render<NavMenu>();

        // Open the menu first
        await cut.InvokeAsync(() => cut.Find("button.neba-menu-toggle").Click());
        cut.Find("ul.neba-nav-menu.active").ShouldNotBeNull();

        // Act — close via JSInvokable method
        await cut.InvokeAsync(() => cut.Instance.CloseMenu());

        // Assert
        cut.FindAll("ul.neba-nav-menu.active").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Navigation should close the open menu when the location changes")]
    public async Task Navigation_ShouldCloseMenu_WhenLocationChanges()
    {
        // Arrange
        var cut = _ctx.Render<NavMenu>();
        await cut.InvokeAsync(() => cut.Find("button.neba-menu-toggle").Click());
        cut.Find("ul.neba-nav-menu.active").ShouldNotBeNull();

        // Act
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/stats");

        // Assert
        await cut.WaitForAssertionAsync(() => cut.FindAll("ul.neba-nav-menu.active").ShouldBeEmpty());
        cut.FindAll("nav.neba-navbar.menu-open").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Navigation should leave the menu collapsed when it is already closed")]
    public void Navigation_ShouldLeaveMenuCollapsed_WhenMenuIsClosed()
    {
        // Arrange
        var cut = _ctx.Render<NavMenu>();

        // Act
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/stats");

        // Assert
        cut.FindAll("ul.neba-nav-menu.active").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Navigation should not close the menu after the component is disposed")]
    public async Task Navigation_ShouldNotCloseMenu_WhenComponentIsDisposed()
    {
        // Arrange
        var cut = _ctx.Render<NavMenu>();
        await cut.InvokeAsync(() => cut.Find("button.neba-menu-toggle").Click());
        await cut.Instance.DisposeAsync();

        // Act
        _ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/stats");

        // Assert
        cut.Find("ul.neba-nav-menu.active").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Dropdown triggers should be keyboard-focusable buttons rather than hash links")]
    public void Render_ShouldRenderDropdownTriggersAsButtons_WhenRendered()
    {
        // Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        var triggers = cut.FindAll("[data-action='toggle-dropdown'] > a[aria-haspopup='true']");
        triggers.Count.ShouldBeGreaterThan(0);
        foreach (var trigger in triggers)
        {
            trigger.GetAttribute("role").ShouldBe("button");
            trigger.GetAttribute("tabindex").ShouldBe("0");
            trigger.HasAttribute("href").ShouldBeFalse();
        }
    }

    [Fact(DisplayName = "CloseMenu should collapse open dropdowns and restore body scrolling through JS")]
    public async Task CloseMenu_ShouldResetMenuUiThroughJs_WhenCalled()
    {
        // Arrange
        var cut = _ctx.Render<NavMenu>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.CloseMenu());

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            _navMenuModule.VerifyInvoke("preventBodyScroll");
            _navMenuModule.VerifyInvoke("closeAllDropdowns");
        });
    }

    [Fact(DisplayName = "CloseMenu should log and continue when the circuit disconnects during the JS reset")]
    public async Task CloseMenu_ShouldLogAndContinue_WhenCircuitDisconnects()
    {
        // Arrange
        var logger = new FakeLogger<NavMenu>();
        _ctx.Services.AddSingleton<ILogger<NavMenu>>(logger);
        _navMenuModule.SetupVoid("closeAllDropdowns").SetException(new JSDisconnectedException("disconnected"));
        var cut = _ctx.Render<NavMenu>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.CloseMenu());

        // Assert
        await cut.WaitForAssertionAsync(() =>
            logger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Information));
        cut.FindAll("ul.neba-nav-menu.active").ShouldBeEmpty();
    }

    [Fact(DisplayName = "CloseMenu should log and continue when the component is disposed during the JS reset")]
    public async Task CloseMenu_ShouldLogAndContinue_WhenComponentIsDisposed()
    {
        // Arrange
        var logger = new FakeLogger<NavMenu>();
        _ctx.Services.AddSingleton<ILogger<NavMenu>>(logger);
        _navMenuModule.SetupVoid("closeAllDropdowns").SetException(new ObjectDisposedException("module"));
        var cut = _ctx.Render<NavMenu>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.CloseMenu());

        // Assert
        await cut.WaitForAssertionAsync(() =>
            logger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Information));
    }

    [Fact(DisplayName = "CloseMenu should log a warning when the JS reset fails")]
    public async Task CloseMenu_ShouldLogWarning_WhenJsResetFails()
    {
        // Arrange
        var logger = new FakeLogger<NavMenu>();
        _ctx.Services.AddSingleton<ILogger<NavMenu>>(logger);
        _navMenuModule.SetupVoid("closeAllDropdowns").SetException(new JSException("failed"));
        var cut = _ctx.Render<NavMenu>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.CloseMenu());

        // Assert
        await cut.WaitForAssertionAsync(() =>
            logger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Warning));
    }

    [Fact(DisplayName = "DisposeAsync should log and continue when the circuit disconnects during JS cleanup")]
    public async Task DisposeAsync_ShouldLogInformation_WhenCircuitDisconnects()
    {
        // Arrange
        var logger = new FakeLogger<NavMenu>();
        _ctx.Services.AddSingleton<ILogger<NavMenu>>(logger);
        _navMenuModule.SetupVoid("dispose").SetException(new JSDisconnectedException("disconnected"));
        var cut = _ctx.Render<NavMenu>();

        // Act
        await cut.Instance.DisposeAsync();

        // Assert
        logger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Information);
    }

    [Fact(DisplayName = "DisposeAsync should log a warning when JS cleanup fails")]
    public async Task DisposeAsync_ShouldLogWarning_WhenJsCleanupFails()
    {
        // Arrange
        var logger = new FakeLogger<NavMenu>();
        _ctx.Services.AddSingleton<ILogger<NavMenu>>(logger);
        _navMenuModule.SetupVoid("dispose").SetException(new JSException("failed"));
        var cut = _ctx.Render<NavMenu>();

        // Act
        await cut.Instance.DisposeAsync();

        // Assert
        logger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Warning);
    }
}