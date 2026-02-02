using Bunit;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Layout;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Layout.NavMenu")]
public sealed class NavMenuTests : IDisposable
{
    private readonly BunitContext _ctx;

    public NavMenuTests()
    {
        _ctx = new BunitContext();

        // Setup bUnit's JSInterop to handle module imports
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./Layout/NavMenu.razor.js");

        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>(MockBehavior.Loose);
        mockWebHostEnvironment.Setup(x => x.EnvironmentName).Returns("Development");

        _ctx.Services.AddSingleton(mockWebHostEnvironment.Object);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render navigation element")]
    public void Render_ShouldContainNavElement_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        var nav = cut.Find("nav");
        nav.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should render menu toggle button with aria attributes")]
    public void Render_ShouldHaveAriaAttributes_OnMenuToggle()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        var toggleButton = cut.Find("button[data-menu-toggle]");
        toggleButton.ShouldNotBeNull();
        // Check that aria-label is present (aria-expanded might be omitted when false)
        toggleButton.GetAttribute("aria-label").ShouldNotBeNull();
        toggleButton.GetAttribute("aria-controls").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should render Tournaments dropdown")]
    public void Render_ShouldContainTournamentsDropdown_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Markup.ShouldContain("Tournaments");
    }

    [Fact(DisplayName = "Should render About dropdown")]
    public void Render_ShouldContainAboutDropdown_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Markup.ShouldContain("About");
    }

    [Fact(DisplayName = "Should render History dropdown")]
    public void Render_ShouldContainHistoryDropdown_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Markup.ShouldContain("History");
    }

    [Fact(DisplayName = "Should have correct aria-label on navigation")]
    public void Render_ShouldHaveAriaLabel_OnNavigation()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        var nav = cut.Find("nav");
        nav.GetAttribute("aria-label").ShouldNotBeNullOrEmpty();
    }

    [Fact(DisplayName = "Should render navigation links")]
    public void Render_ShouldContainNavLinks_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert - NavLink renders as <a> elements with href
        var links = cut.FindAll("a[href]");
        links.Count.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Should render dropdown toggle buttons")]
    public void Render_ShouldContainDropdownToggles_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert - dropdown items have data-action attribute
        var dropdownItems = cut.FindAll("li[data-action='toggle-dropdown']");
        dropdownItems.Count.ShouldBeGreaterThan(0);
    }
}