using Bunit;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Layout;

namespace Neba.Website.Tests.Layout;

[UnitTest]
[Component("Website.Layout.NavMenu")]
public sealed class NavMenuTests : IDisposable
{
    private readonly BunitContext _ctx;

    public NavMenuTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./Layout/NavMenu.razor.js");

        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>(MockBehavior.Strict);
        mockWebHostEnvironment.Setup(x => x.EnvironmentName).Returns("Development");

        _ctx.Services.AddSingleton(mockWebHostEnvironment.Object);
        _ctx.Services.AddSingleton<ILogger<NavMenu>>(NullLogger<NavMenu>.Instance);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render nav element when component is rendered")]
    public void Render_ShouldRenderNavElement_WhenRendered()
    {
        var cut = _ctx.Render<NavMenu>();

        cut.Find("nav.neba-navbar").ShouldNotBeNull();
    }

    [Fact(DisplayName = "ToggleNavMenu should expand the menu when it is collapsed")]
    public async Task ToggleNavMenu_ShouldExpandMenu_WhenMenuIsCollapsed()
    {
        var cut = _ctx.Render<NavMenu>();

        await cut.InvokeAsync(() => cut.Find("button.neba-menu-toggle").Click());

        cut.Find("ul.neba-nav-menu.active").ShouldNotBeNull();
        cut.Find("nav.neba-navbar.menu-open").ShouldNotBeNull();
    }

    [Fact(DisplayName = "CloseMenu should collapse the navigation menu when called")]
    public async Task CloseMenu_ShouldCollapseMenu_WhenCalled()
    {
        var cut = _ctx.Render<NavMenu>();

        // Open the menu first
        await cut.InvokeAsync(() => cut.Find("button.neba-menu-toggle").Click());
        cut.Find("ul.neba-nav-menu.active").ShouldNotBeNull();

        // Close via JSInvokable method
        await cut.InvokeAsync(() => cut.Instance.CloseMenu());

        cut.FindAll("ul.neba-nav-menu.active").ShouldBeEmpty();
    }
}