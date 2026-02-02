using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.NebaLoadingIndicator")]
public sealed class NebaLoadingIndicatorTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should not render when IsVisible is false")]
    public void Render_ShouldBeEmpty_WhenIsVisibleIsFalse()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, false));

        // Assert
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render loading indicator after delay when IsVisible is true")]
    public async Task Render_ShouldShowOverlay_WhenIsVisibleIsTrue()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0)); // No delay for immediate rendering

        // Wait for the component to render
        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Assert - should have the overlay element
        cut.FindAll(".neba-loading-overlay, .neba-loading-overlay-section").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should display custom text when provided")]
    public async Task Render_ShouldDisplayText_WhenTextProvided()
    {
        // Arrange
        const string customText = "Loading data...";

        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0)
            .Add(p => p.Text, customText));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Assert
        cut.Markup.ShouldContain(customText);
        cut.Find(".neba-loading-text").TextContent.ShouldBe(customText);
    }

    [Fact(DisplayName = "Should not display text element when Text is null")]
    public async Task Render_ShouldHideTextElement_WhenTextIsNull()
    {
        // Arrange
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0)
            .Add(p => p.Text, null));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Assert
        cut.FindAll(".neba-loading-text").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should apply page scope class by default")]
    public async Task Render_ShouldApplyPageScopeClass_WhenScopeIsDefault()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Assert
        cut.Find(".neba-loading-overlay-page").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should apply fullscreen scope class")]
    public async Task Render_ShouldApplyFullscreenClass_WhenScopeIsFullScreen()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0)
            .Add(p => p.Scope, LoadingIndicatorScope.FullScreen));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Assert
        cut.Find(".neba-loading-overlay-fullscreen").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should apply section scope class")]
    public async Task Render_ShouldApplySectionClass_WhenScopeIsSection()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0)
            .Add(p => p.Scope, LoadingIndicatorScope.Section));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Assert
        cut.Find(".neba-loading-overlay-section").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should render loading wave animation")]
    public async Task Render_ShouldShowLoadingWave_WhenVisible()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Assert
        cut.Find(".neba-loading-wave").ShouldNotBeNull();
        cut.Find(".neba-loading-wave-animated").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should invoke OnOverlayClick when overlay is clicked")]
    public async Task OnOverlayClick_ShouldInvokeCallback_WhenOverlayClicked()
    {
        // Arrange
        var clicked = false;

        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0)
            .Add(p => p.Scope, LoadingIndicatorScope.FullScreen)
            .Add(p => p.OnOverlayClick, () => { clicked = true; }));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Act
        var overlay = cut.Find(".neba-loading-overlay");
        await overlay.ClickAsync();

        // Assert
        clicked.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should respect delay before showing indicator")]
    public void Render_ShouldBeEmpty_WhenDelayNotElapsed()
    {
        // Arrange & Act - use a longer delay
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 500));

        // Assert - should not be visible immediately
        cut.Markup.Trim().ShouldBeEmpty();
    }
}