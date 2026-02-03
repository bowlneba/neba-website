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

        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>(MockBehavior.Strict);
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

    [Fact(DisplayName = "Should toggle menu when button is clicked")]
    public void ToggleNavMenu_ShouldToggleMenu_WhenClicked()
    {
        // Arrange
        var cut = _ctx.Render<NavMenu>();
        var initialMarkup = cut.Markup;

        // Act
        var toggleButton = cut.Find("button[data-menu-toggle]");
        toggleButton.Click();

        // Assert
        cut.Markup.ShouldNotBe(initialMarkup, "Menu state should change after toggle");
    }

    [Fact(DisplayName = "Should render logo image")]
    public void Render_ShouldContainLogo_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        var logo = cut.Find("img.navbar-logo");
        logo.ShouldNotBeNull();
        logo.GetAttribute("src").ShouldBe("images/neba-1963.png");
        logo.GetAttribute("alt").ShouldBe("NEBA | 1963");
    }

    [Fact(DisplayName = "Should render navbar wrapper")]
    public void Render_ShouldContainNavbarWrapper_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        var wrapper = cut.Find("div.neba-navbar-wrapper");
        wrapper.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should have role navigation on nav element")]
    public void Render_ShouldHaveNavigationRole_OnNavElement()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        var nav = cut.Find("nav[role='navigation']");
        nav.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should render Stats link")]
    public void Render_ShouldContainStatsLink_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Markup.ShouldContain("Stats");
    }

    [Fact(DisplayName = "Should render News link")]
    public void Render_ShouldContainNewsLink_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Markup.ShouldContain("News");
    }

    [Fact(DisplayName = "Should render Hall of Fame link")]
    public void Render_ShouldContainHallOfFameLink_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Markup.ShouldContain("Hall of Fame");
    }

    [Fact(DisplayName = "Should render Sponsors link")]
    public void Render_ShouldContainSponsorsLink_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Markup.ShouldContain("Sponsors");
    }

    [Fact(DisplayName = "Should render Centers link")]
    public void Render_ShouldContainCentersLink_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Markup.ShouldContain("Centers");
    }

    [Fact(DisplayName = "Should render menu with main-menu id")]
    public void Render_ShouldHaveMainMenuId_OnMenu()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        var menu = cut.Find("ul#main-menu");
        menu.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should render Future Tournaments link")]
    public void Render_ShouldContainFutureTournamentsLink_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Markup.ShouldContain("Future Tournaments");
    }

    [Fact(DisplayName = "Should render Tournament Rules link")]
    public void Render_ShouldContainTournamentRulesLink_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Markup.ShouldContain("Tournament Rules");
    }

    [Fact(DisplayName = "Should render Bylaws link")]
    public void Render_ShouldContainBylawsLink_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Markup.ShouldContain("Bylaws");
    }

    [Fact(DisplayName = "Should render Champions link")]
    public void Render_ShouldContainChampionsLink_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Markup.ShouldContain("Champions");
    }

    [Fact(DisplayName = "Should render Bowler of the Year link")]
    public void Render_ShouldContainBowlerOfTheYearLink_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Markup.ShouldContain("Bowler of the Year");
    }

    [Fact(DisplayName = "Should render High Average link")]
    public void Render_ShouldContainHighAverageLink_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Markup.ShouldContain("High Average");
    }

    [Fact(DisplayName = "Should render High Block link")]
    public void Render_ShouldContainHighBlockLink_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Markup.ShouldContain("High Block");
    }

    [Fact(DisplayName = "Should have correct home page link")]
    public void Render_ShouldHaveCorrectHomeLink_InBrand()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        var brandLink = cut.Find("a.navbar-brand");
        brandLink.GetAttribute("href").ShouldBe("/");
    }

    [Fact(DisplayName = "Should have dropdown links with role menuitem")]
    public void Render_ShouldHaveCorrectDropdownRoles_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        var dropdownLinks = cut.FindAll("a.neba-dropdown-link[role='menuitem']");
        dropdownLinks.Count.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Should have dropdowns with role menu")]
    public void Render_ShouldHaveMenuRole_OnDropdowns()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        var dropdowns = cut.FindAll("div.neba-dropdown[role='menu']");
        dropdowns.Count.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Should display Past Tournaments link with current year")]
    public void Render_ShouldContainPastTournamentsLink_WithCurrentYear()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();
        var currentYear = DateTime.Now.Year;

        // Assert
        cut.Markup.ShouldContain($"tournaments/{currentYear}");
    }

    [Fact(DisplayName = "Should render navbar with correct CSS class")]
    public void Render_ShouldHaveCorrectNavbarClass_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        var navbar = cut.Find("nav.neba-navbar");
        navbar.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should render dropdown divider")]
    public void Render_ShouldContainDropdownDivider_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        var dividers = cut.FindAll("div.neba-dropdown-divider");
        dividers.Count.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Should not have active class when menu is collapsed")]
    public void Render_ShouldNotHaveActiveClass_WhenMenuCollapsed()
    {
        // Arrange & Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        var menu = cut.Find("ul#main-menu");
        menu.ClassList.ShouldNotContain("active");
    }

    [Fact(DisplayName = "Should add active class when menu is toggled")]
    public void ToggleMenu_ShouldAddActiveClass_WhenButtonClicked()
    {
        // Arrange
        var cut = _ctx.Render<NavMenu>();

        // Act
        var toggleButton = cut.Find("button.neba-menu-toggle");
        toggleButton.Click();

        // Assert
        var menu = cut.Find("ul#main-menu");
        menu.ClassList.ShouldContain("active");
    }

    [Fact(DisplayName = "Should add menu-open class to navbar when menu toggled")]
    public void ToggleMenu_ShouldAddMenuOpenClass_ToNavbar()
    {
        // Arrange
        var cut = _ctx.Render<NavMenu>();

        // Act
        var toggleButton = cut.Find("button.neba-menu-toggle");
        toggleButton.Click();

        // Assert
        var navbar = cut.Find("nav.neba-navbar");
        navbar.ClassList.ShouldContain("menu-open");
    }

    [Fact(DisplayName = "Should close menu when CloseMenu is invoked")]
    public async Task CloseMenu_ShouldRemoveActiveClass_WhenInvoked()
    {
        // Arrange
        var cut = _ctx.Render<NavMenu>();

        // Open the menu first
        var toggleButton = cut.Find("button.neba-menu-toggle");
        await toggleButton.ClickAsync();

        var menu = cut.Find("ul#main-menu");
        menu.ClassList.ShouldContain("active");

        // Act - Get the component instance and call CloseMenu via InvokeAsync
        var component = cut.Instance;
        await cut.InvokeAsync(() => component.CloseMenu());

        // Assert
        menu = cut.Find("ul#main-menu");
        menu.ClassList.ShouldNotContain("active");
    }

    [Fact(DisplayName = "Should remove menu-open class from navbar when CloseMenu invoked")]
    public async Task CloseMenu_ShouldRemoveMenuOpenClass_FromNavbar()
    {
        // Arrange
        var cut = _ctx.Render<NavMenu>();

        // Open the menu first
        var toggleButton = cut.Find("button.neba-menu-toggle");
        await toggleButton.ClickAsync();

        var navbar = cut.Find("nav.neba-navbar");
        navbar.ClassList.ShouldContain("menu-open");

        // Act
        var component = cut.Instance;
        await cut.InvokeAsync(() => component.CloseMenu());

        // Assert
        navbar = cut.Find("nav.neba-navbar");
        navbar.ClassList.ShouldNotContain("menu-open");
    }
}