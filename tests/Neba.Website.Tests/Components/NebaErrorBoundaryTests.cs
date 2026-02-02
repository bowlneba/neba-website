using Bunit;

using Microsoft.AspNetCore.Components;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.NebaErrorBoundary")]
public sealed class NebaErrorBoundaryTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render child content when no error occurs")]
    public void Render_ShouldDisplayChildContent_WhenNoErrorOccurs()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaErrorBoundary>(parameters => parameters
            .AddChildContent("<p>Child content here</p>"));

        // Assert
        cut.Markup.ShouldContain("Child content here");
        cut.FindAll(".neba-panel").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should display error panel when child component throws")]
    public void Render_ShouldDisplayErrorPanel_WhenChildThrows()
    {
        // Arrange
        var cut = _ctx.Render<NebaErrorBoundary>(parameters => parameters
            .AddChildContent<ThrowingComponent>());

        // Assert
        cut.FindAll(".neba-panel").Count.ShouldBe(1);
        cut.Markup.ShouldContain("Something went wrong");
    }

    [Fact(DisplayName = "Should display custom message when provided")]
    public void Render_ShouldDisplayCustomMessage_WhenCustomMessageProvided()
    {
        // Arrange
        const string customMessage = "A custom error occurred!";

        var cut = _ctx.Render<NebaErrorBoundary>(parameters => parameters
            .Add(p => p.CustomMessage, customMessage)
            .AddChildContent<ThrowingComponent>());

        // Assert
        cut.Markup.ShouldContain(customMessage);
    }

    [Fact(DisplayName = "Should hide error details toggle button when ShowDetails is false")]
    public void Render_ShouldHideDetailsToggle_WhenShowDetailsIsFalse()
    {
        // Arrange
        var cut = _ctx.Render<NebaErrorBoundary>(parameters => parameters
            .Add(p => p.ShowDetails, false)
            .AddChildContent<ThrowingComponent>());

        // Assert - should not have the show/hide details button
        var buttons = cut.FindAll("button.neba-btn-secondary");
        buttons.Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should show details toggle button when ShowDetails is true")]
    public void Render_ShouldShowDetailsToggle_WhenShowDetailsIsTrue()
    {
        // Arrange
        var cut = _ctx.Render<NebaErrorBoundary>(parameters => parameters
            .Add(p => p.ShowDetails, true)
            .AddChildContent<ThrowingComponent>());

        // Assert - should have the show/hide details button
        var detailsButton = cut.Find("button.neba-btn-secondary");
        detailsButton.ShouldNotBeNull();
        detailsButton.TextContent.ShouldContain("Details");
    }

    [Fact(DisplayName = "Should toggle error details visibility when button clicked")]
    public void ToggleDetails_ShouldShowStackTrace_WhenButtonClicked()
    {
        // Arrange
        var cut = _ctx.Render<NebaErrorBoundary>(parameters => parameters
            .Add(p => p.ShowDetails, true)
            .AddChildContent<ThrowingComponent>());

        // Initially details are hidden
        cut.FindAll("pre").Count.ShouldBe(0);

        // Act - click toggle button
        var toggleButton = cut.Find("button.neba-btn-secondary");
        toggleButton.Click();

        // Assert - details should be visible
        cut.FindAll("pre").Count.ShouldBe(1);
        cut.Markup.ShouldContain("Test exception");
    }

    [Fact(DisplayName = "Should render Try Again button for recovery")]
    public void Render_ShouldShowTryAgainButton_WhenErrorOccurs()
    {
        // Arrange
        var cut = _ctx.Render<NebaErrorBoundary>(parameters => parameters
            .AddChildContent<ThrowingComponent>());

        // Assert
        var tryAgainButton = cut.Find("button.neba-btn-primary");
        tryAgainButton.TextContent.ShouldContain("Reload Page");
    }

    /// <summary>
    /// Helper component that always throws an exception during rendering.
    /// </summary>
    private sealed class ThrowingComponent : ComponentBase
    {
        protected override void OnInitialized()
        {
            throw new InvalidOperationException("Test exception");
        }
    }
}