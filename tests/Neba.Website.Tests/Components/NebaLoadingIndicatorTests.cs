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
            .Add(p => p.OnOverlayClick, () => clicked = true));

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

    [Fact(DisplayName = "Should hide indicator when IsVisible changes from true to false")]
    public async Task Render_ShouldHideOverlay_WhenIsVisibleChangesFromTrueToFalse()
    {
        // Arrange
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();
        cut.FindAll(".neba-loading-overlay, .neba-loading-overlay-section").Count.ShouldBe(1);

        // Act - render again with IsVisible false
        cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, false)
            .Add(p => p.DelayMs, 0));

        // Assert
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should display empty text element when text is empty string")]
    public async Task Render_ShouldNotDisplayEmptyTextElement_WhenTextIsEmptyString()
    {
        // Arrange
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0)
            .Add(p => p.Text, ""));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Assert
        cut.FindAll(".neba-loading-text").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should display whitespace text element when text is whitespace")]
    public async Task Render_ShouldNotDisplayWhitespaceTextElement_WhenTextIsWhitespace()
    {
        // Arrange
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0)
            .Add(p => p.Text, "   "));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Assert
        cut.FindAll(".neba-loading-text").Count.ShouldBe(0);
    }

    [Theory(DisplayName = "Should apply correct scope class")]
    [InlineData(LoadingIndicatorScope.Page, "neba-loading-overlay neba-loading-overlay-page", TestDisplayName = "Page scope")]
    [InlineData(LoadingIndicatorScope.FullScreen, "neba-loading-overlay neba-loading-overlay-fullscreen", TestDisplayName = "FullScreen scope")]
    [InlineData(LoadingIndicatorScope.Section, "neba-loading-overlay-section", TestDisplayName = "Section scope")]
    public async Task Render_ShouldApplyCorrectScopeClass_ForEachScope(LoadingIndicatorScope scope, string expectedClass)
    {
        // Arrange
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0)
            .Add(p => p.Scope, scope));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Assert
        var overlay = cut.Find("[class*='neba-loading-overlay']");
        overlay.GetAttribute("class").ShouldBe(expectedClass);
    }

    [Fact(DisplayName = "Should render five wave divs for animation")]
    public async Task Render_ShouldRenderFiveWaveDivs_WhenVisible()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Assert
        var wave = cut.Find(".neba-loading-wave");
        var divs = wave.QuerySelectorAll("div");
        divs.Length.ShouldBe(5);
    }

    [Fact(DisplayName = "Should not invoke OnOverlayClick when overlay is not clicked")]
    public async Task OnOverlayClick_ShouldNotInvokeCallback_WhenNotClicked()
    {
        // Arrange
        var clicked = false;

        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0)
            .Add(p => p.OnOverlayClick, () => clicked = true));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Assert - no click action performed
        clicked.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should maintain state consistency when visibility toggled multiple times")]
    public async Task Render_ShouldMaintainStateConsistency_WhenToggledMultipleTimes()
    {
        // Arrange & Act - first render with IsVisible false
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, false)
            .Add(p => p.DelayMs, 0));

        cut.Markup.Trim().ShouldBeEmpty();

        // Re-render with IsVisible true
        cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0));
        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.FindAll(".neba-loading-overlay, .neba-loading-overlay-section").Count.ShouldBe(1);

        // Re-render with IsVisible false
        cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, false)
            .Add(p => p.DelayMs, 0));
        cut.Markup.Trim().ShouldBeEmpty();

        // Re-render with IsVisible true again
        cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0));
        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.FindAll(".neba-loading-overlay, .neba-loading-overlay-section").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should display different text in different renders")]
    public async Task Render_ShouldDisplayDifferentText_InDifferentRenders()
    {
        // Arrange & Act - first render with text
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0)
            .Add(p => p.Text, "Loading..."));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();
        cut.Find(".neba-loading-text").TextContent.ShouldBe("Loading...");

        // Re-render with different text
        cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0)
            .Add(p => p.Text, "Processing..."));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();
        cut.Find(".neba-loading-text").TextContent.ShouldBe("Processing...");
    }

    [Fact(DisplayName = "Should apply different scopes in different renders")]
    public async Task Render_ShouldApplyDifferentScopes_InDifferentRenders()
    {
        // Arrange & Act - first render with Page scope
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0)
            .Add(p => p.Scope, LoadingIndicatorScope.Page));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();
        cut.Find(".neba-loading-overlay-page").ShouldNotBeNull();

        // Re-render with FullScreen scope
        cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0)
            .Add(p => p.Scope, LoadingIndicatorScope.FullScreen));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();
        cut.Find(".neba-loading-overlay-fullscreen").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should handle immediate visibility toggle")]
    public async Task Render_ShouldHandleImmediateToggle_WithoutDelay()
    {
        // Arrange
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0));

        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Act - toggle immediately
        cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, false)
            .Add(p => p.DelayMs, 0));

        // Assert
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should respect minimum display time when hiding")]
    public async Task Render_ShouldRespectMinimumDisplayTime_WhenHiding()
    {
        // Arrange - render with IsVisible true and short minimum display time
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0)
            .Add(p => p.MinimumDisplayMs, 300));

        // Wait for it to show
        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();
        cut.FindAll(".neba-loading-overlay, .neba-loading-overlay-section").Count.ShouldBe(1);

        // Act - immediately try to hide it (before minimum time elapsed)
        cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, false)
            .Add(p => p.DelayMs, 0)
            .Add(p => p.MinimumDisplayMs, 300));

        // Assert - should still be visible initially
        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Should eventually hide after minimum display time
        await Task.Delay(350, Xunit.TestContext.Current.CancellationToken);
        cut.Render();
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should hide immediately if minimum display time already elapsed")]
    public async Task Render_ShouldHideImmediately_WhenMinimumDisplayTimeElapsed()
    {
        // Arrange - render with IsVisible true
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 0)
            .Add(p => p.MinimumDisplayMs, 100));

        // Wait for it to show and exceed minimum display time
        await Task.Delay(200, Xunit.TestContext.Current.CancellationToken);
        cut.Render();
        cut.FindAll(".neba-loading-overlay, .neba-loading-overlay-section").Count.ShouldBe(1);

        // Act - hide after minimum time has passed
        cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, false)
            .Add(p => p.DelayMs, 0));

        // Assert - should hide immediately since minimum time already elapsed
        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        cut.Render();
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should cancel delay timer when hiding before delay expires")]
    public async Task Render_ShouldCancelDelayTimer_WhenHidingBeforeDelayExpires()
    {
        // Arrange - render with long delay
        var cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.DelayMs, 500));

        // Verify not shown yet
        cut.Markup.Trim().ShouldBeEmpty();

        // Act - hide before delay expires
        cut = _ctx.Render<NebaLoadingIndicator>(parameters => parameters
            .Add(p => p.IsVisible, false)
            .Add(p => p.DelayMs, 500));

        // Wait past original delay time
        await Task.Delay(600, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Assert - should never have shown
        cut.Markup.Trim().ShouldBeEmpty();
    }
}