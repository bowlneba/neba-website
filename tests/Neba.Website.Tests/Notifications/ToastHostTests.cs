using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Notifications;

namespace Neba.Website.Tests.Notifications;

[UnitTest]
[Component("Website.Notifications.ToastHost")]
public sealed class ToastHostTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly ToastService _toastService = new();

    public ToastHostTests()
    {
        _ctx.Services.AddScoped(_ => _toastService);
        _ctx.Services.AddRouting();
        _ctx.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    public void Dispose()
    {
        _toastService.Dispose();
        _ctx.Dispose();
    }

    [Fact(DisplayName = "Should render nothing when no notification is active")]
    public void Render_ShouldRenderNothing_WhenNoNotificationIsActive()
    {
        // Arrange & Act
        var cut = _ctx.Render<ToastHost>();

        // Assert
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render NebaToast when a notification is active")]
    public void Render_ShouldRenderNebaToast_WhenNotificationIsActive()
    {
        // Arrange
        var cut = _ctx.Render<ToastHost>();

        // Act
        _toastService.Show("Cache Cleared", "5 entries evicted.", NotifySeverity.Success);
        cut.WaitForState(() => cut.Markup.Contains("Cache Cleared", StringComparison.Ordinal));

        // Assert
        cut.Markup.ShouldContain("Cache Cleared");
        cut.Markup.ShouldContain("5 entries evicted.");
    }

    [Fact(DisplayName = "Should clear toast when dismissed")]
    public void Render_ShouldClearToast_AfterDismiss()
    {
        // Arrange
        var cut = _ctx.Render<ToastHost>();
        _toastService.Show("Title", "Message", NotifySeverity.Info);
        cut.WaitForState(() => cut.Markup.Contains("Title", StringComparison.Ordinal));

        // Act
        _toastService.Dismiss();
        cut.WaitForState(() => !cut.Markup.Contains("Title", StringComparison.Ordinal));

        // Assert
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show a success toast and strip the loggedOut marker when present in the query string")]
    public void OnInitialized_ShouldShowLoggedOutToastAndStripQuery_WhenLoggedOutMarkerPresent()
    {
        // Arrange
        var navigationManager = _ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("loggedOut", "1"));

        // Act
        var cut = _ctx.Render<ToastHost>();
        cut.WaitForState(() => cut.Markup.Contains("Logged Out", StringComparison.Ordinal));

        // Assert
        cut.Markup.ShouldContain("Logged Out");
        cut.Markup.ShouldContain("You have been logged out.");
        navigationManager.Uri.ShouldNotContain("loggedOut");
    }

    [Fact(DisplayName = "Should show an error toast and strip the logoutError marker when present in the query string")]
    public void OnInitialized_ShouldShowLogoutErrorToastAndStripQuery_WhenLogoutErrorMarkerPresent()
    {
        // Arrange
        var navigationManager = _ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("logoutError", "1"));

        // Act
        var cut = _ctx.Render<ToastHost>();
        cut.WaitForState(() => cut.Markup.Contains("Logout Error", StringComparison.Ordinal));

        // Assert
        cut.Markup.ShouldContain("Logout Error");
        cut.Markup.ShouldContain("You were signed out, but the server logout request failed.");
        navigationManager.Uri.ShouldNotContain("logoutError");
    }

    [Fact(DisplayName = "Should preserve unrelated query parameters when stripping the redirect toast marker")]
    public void OnInitialized_ShouldPreserveUnrelatedQueryParameters_WhenStrippingMarker()
    {
        // Arrange
        var navigationManager = _ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameters(
            new Dictionary<string, object?> { ["loggedOut"] = "1", ["keep"] = "value" }));

        // Act
        var cut = _ctx.Render<ToastHost>();
        cut.WaitForState(() => cut.Markup.Contains("Logged Out", StringComparison.Ordinal));

        // Assert
        navigationManager.Uri.ShouldContain("keep=value");
        navigationManager.Uri.ShouldNotContain("loggedOut");
    }

    [Fact(DisplayName = "Should not show a toast when the renderer is not interactive, even if a redirect marker is present")]
    public void OnInitialized_ShouldNotShowToast_WhenRendererIsNotInteractive()
    {
        // Arrange
        _ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var navigationManager = _ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("loggedOut", "1"));

        // Act
        var cut = _ctx.Render<ToastHost>();

        // Assert
        cut.Markup.Trim().ShouldBeEmpty();
    }
}