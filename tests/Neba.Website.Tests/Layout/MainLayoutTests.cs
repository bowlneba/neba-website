using Bunit;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Layout;

namespace Neba.Website.Tests.Layout;

[UnitTest]
[Component("Website.Layout.MainLayout")]
public sealed class MainLayoutTests : IDisposable
{
    private readonly BunitContext _ctx;

    public MainLayoutTests()
    {
        _ctx = new BunitContext();

        // Setup bUnit's JSInterop to handle module imports (used by NavMenu)
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./Layout/NavMenu.razor.js");

        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>(MockBehavior.Loose);
        mockWebHostEnvironment.Setup(x => x.EnvironmentName).Returns("Development");

        _ctx.Services.AddSingleton(mockWebHostEnvironment.Object);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render skip-to-content link for accessibility")]
    public void Render_ShouldContainSkipLink_ForAccessibility()
    {
        // Arrange & Act
        var cut = _ctx.Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, builder => builder.AddContent(0, "Test content")));

        // Assert
        var skipLink = cut.Find("a.skip-link");
        skipLink.ShouldNotBeNull();
        skipLink.GetAttribute("href").ShouldBe("#main-content");
    }

    [Fact(DisplayName = "Should render header with NEBA branding")]
    public void Render_ShouldContainNebaBranding_InHeader()
    {
        // Arrange & Act
        var cut = _ctx.Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, builder => builder.AddContent(0, "Test content")));

        // Assert
        var header = cut.Find("header");
        header.ShouldNotBeNull();
        cut.Markup.ShouldContain("NEBA");
    }

    [Fact(DisplayName = "Should render main content area with correct id")]
    public void Render_ShouldHaveMainContentId_OnMainElement()
    {
        // Arrange & Act
        var cut = _ctx.Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, builder => builder.AddContent(0, "Test content")));

        // Assert
        var main = cut.Find("main#main-content");
        main.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should render child content in body")]
    public void Render_ShouldDisplayChildContent_WhenBodyProvided()
    {
        // Arrange
        const string childContent = "This is the page content";

        // Act
        var cut = _ctx.Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, builder => builder.AddContent(0, childContent)));

        // Assert
        cut.Markup.ShouldContain(childContent);
    }

    [Fact(DisplayName = "Should render footer")]
    public void Render_ShouldContainFooter_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, builder => builder.AddContent(0, "Test content")));

        // Assert
        var footer = cut.Find("footer");
        footer.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should render footer links")]
    public void Render_ShouldContainFooterLinks_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, builder => builder.AddContent(0, "Test content")));

        // Assert
        cut.Markup.ShouldContain("Privacy");
        cut.Markup.ShouldContain("Terms");
        cut.Markup.ShouldContain("Contact");
    }

    [Fact(DisplayName = "Should include NavMenu component")]
    public void Render_ShouldContainNavMenu_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, builder => builder.AddContent(0, "Test content")));

        // Assert - NavMenu should be rendered within the layout
        var nav = cut.Find("nav");
        nav.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should have blazor-error-ui element for connection errors")]
    public void Render_ShouldContainBlazorErrorUi_ForConnectionErrors()
    {
        // Arrange & Act
        var cut = _ctx.Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, builder => builder.AddContent(0, "Test content")));

        // Assert
        var errorUi = cut.Find("#blazor-error-ui");
        errorUi.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should have proper landmark elements")]
    public void Render_ShouldContainLandmarkElements_ForAccessibility()
    {
        // Arrange & Act
        var cut = _ctx.Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, builder => builder.AddContent(0, "Test content")));

        // Assert - Check for semantic HTML elements that provide implicit roles
        cut.FindAll("header").Count.ShouldBeGreaterThan(0);
        cut.FindAll("main").Count.ShouldBeGreaterThan(0);
        cut.FindAll("footer").Count.ShouldBeGreaterThan(0);
        cut.FindAll("nav").Count.ShouldBeGreaterThan(0);
    }
}