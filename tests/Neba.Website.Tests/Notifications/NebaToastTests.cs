using Bunit;

using Microsoft.AspNetCore.Components;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Notifications;

namespace Neba.Website.Tests.Notifications;

[UnitTest]
[Component("Website.Notifications.NebaToast")]
public sealed class NebaToastTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render title and message")]
    public void Render_ShouldRenderTitleAndMessage_WhenProvided()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaToast>(parameters => parameters
            .Add(p => p.Title, "API Cache Cleared")
            .Add(p => p.Message, "Cache cleared."));

        // Assert
        cut.Markup.ShouldContain("API Cache Cleared");
        cut.Markup.ShouldContain("Cache cleared.");
    }

    [Theory(DisplayName = "Should render severity css class")]
    [InlineData(NotifySeverity.Success, "neba-toast-success", TestDisplayName = "Success")]
    [InlineData(NotifySeverity.Warning, "neba-toast-warning", TestDisplayName = "Warning")]
    [InlineData(NotifySeverity.Error, "neba-toast-error", TestDisplayName = "Error")]
    [InlineData(NotifySeverity.Info, "neba-toast-info", TestDisplayName = "Info")]
    [InlineData(NotifySeverity.Normal, "neba-toast-normal", TestDisplayName = "Normal")]
    public void Render_ShouldRenderSeverityCssClass_WhenSeverityIsSet(NotifySeverity severity, string expectedClass)
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaToast>(parameters => parameters
            .Add(p => p.Title, "Title")
            .Add(p => p.Message, "Message")
            .Add(p => p.Severity, severity));

        // Assert
        var toast = cut.Find(".neba-toast");
        var classes = toast.GetAttribute("class");

        classes.ShouldNotBeNull();
        classes.ShouldContain(expectedClass);
    }

    [Fact(DisplayName = "Should invoke dismiss callback when dismiss button is clicked")]
    public void Render_ShouldInvokeDismissCallback_WhenDismissIsClicked()
    {
        // Arrange
        var wasDismissed = false;

        var cut = _ctx.Render<NebaToast>(parameters => parameters
            .Add(p => p.Title, "Title")
            .Add(p => p.Message, "Message")
            .Add(p => p.OnDismiss, EventCallback.Factory.Create(this, () => wasDismissed = true)));

        // Act
        cut.Find("button.neba-toast-dismiss").Click();

        // Assert
        wasDismissed.ShouldBeTrue();
    }
}