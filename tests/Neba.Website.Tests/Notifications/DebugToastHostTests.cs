using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Notifications;

namespace Neba.Website.Tests.Notifications;

[UnitTest]
[Component("Website.Notifications.DebugToastHost")]
public sealed class DebugToastHostTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly DebugToastService _toastService = new();

    public DebugToastHostTests()
    {
        _ctx.Services.AddScoped(_ => _toastService);
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
        var cut = _ctx.Render<DebugToastHost>();

        // Assert
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render NebaToast when a notification is active")]
    public void Render_ShouldRenderNebaToast_WhenNotificationIsActive()
    {
        // Arrange
        var cut = _ctx.Render<DebugToastHost>();

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
        var cut = _ctx.Render<DebugToastHost>();
        _toastService.Show("Title", "Message", NotifySeverity.Info);
        cut.WaitForState(() => cut.Markup.Contains("Title", StringComparison.Ordinal));

        // Act
        _toastService.Dismiss();
        cut.WaitForState(() => !cut.Markup.Contains("Title", StringComparison.Ordinal));

        // Assert
        cut.Markup.Trim().ShouldBeEmpty();
    }
}