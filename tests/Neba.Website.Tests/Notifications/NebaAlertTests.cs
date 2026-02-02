using Bunit;

using Microsoft.AspNetCore.Components;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Notifications;

namespace Neba.Website.Tests.Notifications;

[UnitTest]
[Component("Website.Notifications.NebaAlert")]
public sealed class NebaAlertTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Theory(DisplayName = "Should render alert with correct severity class")]
    [InlineData(NotifySeverity.Error, "error", TestDisplayName = "Error severity")]
    [InlineData(NotifySeverity.Warning, "warning", TestDisplayName = "Warning severity")]
    [InlineData(NotifySeverity.Success, "success", TestDisplayName = "Success severity")]
    [InlineData(NotifySeverity.Info, "info", TestDisplayName = "Info severity")]
    [InlineData(NotifySeverity.Normal, "normal", TestDisplayName = "Normal severity")]
    public void Render_ShouldApplyCorrectSeverityClass(NotifySeverity severity, string expectedClass)
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaAlert>(parameters => parameters
            .Add(p => p.Severity, severity)
            .Add(p => p.Message, "Test message"));

        // Assert
        cut.Markup.ShouldContain($"neba-alert-{expectedClass}");
    }

    [Theory(DisplayName = "Should render alert with correct variant class")]
    [InlineData(AlertVariant.Filled, "filled", TestDisplayName = "Filled variant")]
    [InlineData(AlertVariant.Outlined, "outlined", TestDisplayName = "Outlined variant")]
    [InlineData(AlertVariant.Dense, "dense", TestDisplayName = "Dense variant")]
    public void Render_ShouldApplyCorrectVariantClass(AlertVariant variant, string expectedClass)
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaAlert>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Info)
            .Add(p => p.Message, "Test message")
            .Add(p => p.Variant, variant));

        // Assert
        cut.Markup.ShouldContain($"neba-alert-{expectedClass}");
    }

    [Fact(DisplayName = "Should display title when provided")]
    public void Render_ShouldDisplayTitle_WhenTitleIsProvided()
    {
        // Arrange
        const string title = "Alert Title";

        // Act
        var cut = _ctx.Render<NebaAlert>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Info)
            .Add(p => p.Message, "Test message")
            .Add(p => p.Title, title));

        // Assert
        cut.Markup.ShouldContain($"<div class=\"neba-alert-title");
        cut.Markup.ShouldContain(title);
    }

    [Fact(DisplayName = "Should not display title element when title is null")]
    public void Render_ShouldNotDisplayTitle_WhenTitleIsNull()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaAlert>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Info)
            .Add(p => p.Message, "Test message")
            .Add(p => p.Title, null));

        // Assert
        cut.FindAll(".neba-alert-title").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should display main message")]
    public void Render_ShouldDisplayMessage_WhenMessageIsProvided()
    {
        // Arrange
        const string message = "This is a test message";

        // Act
        var cut = _ctx.Render<NebaAlert>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Info)
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.ShouldContain(message);
    }

    [Fact(DisplayName = "Should display icon by default")]
    public void Render_ShouldDisplayIcon_ByDefault()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaAlert>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Info)
            .Add(p => p.Message, "Test message"));

        // Assert
        cut.FindAll(".neba-alert-icon").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should not display icon when ShowIcon is false")]
    public void Render_ShouldNotDisplayIcon_WhenShowIconIsFalse()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaAlert>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Info)
            .Add(p => p.Message, "Test message")
            .Add(p => p.ShowIcon, false));

        // Assert
        cut.FindAll(".neba-alert-icon").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should not display close button when Dismissible is false")]
    public void Render_ShouldNotDisplayCloseButton_WhenDismissibleIsFalse()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaAlert>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Info)
            .Add(p => p.Message, "Test message")
            .Add(p => p.Dismissible, false));

        // Assert
        cut.FindAll("button.neba-alert-close").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should display close button when Dismissible is true")]
    public void Render_ShouldDisplayCloseButton_WhenDismissibleIsTrue()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaAlert>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Info)
            .Add(p => p.Message, "Test message")
            .Add(p => p.Dismissible, true));

        // Assert
        cut.FindAll("button.neba-alert-close").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should display validation messages as list")]
    public void Render_ShouldDisplayValidationMessages_WhenProvided()
    {
        // Arrange
        var messages = new[] { "Error 1", "Error 2", "Error 3" };

        // Act
        var cut = _ctx.Render<NebaAlert>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Error)
            .Add(p => p.Message, "Test message")
            .Add(p => p.ValidationMessages, messages));

        // Assert
        var list = cut.Find("ul.neba-alert-validation-list");
        list.ShouldNotBeNull();

        var items = cut.FindAll("ul.neba-alert-validation-list li");
        items.Count.ShouldBe(3);
        items[0].TextContent.ShouldBe("Error 1");
        items[1].TextContent.ShouldBe("Error 2");
        items[2].TextContent.ShouldBe("Error 3");
    }

    [Fact(DisplayName = "Should display message instead of validation list when ValidationMessages is empty")]
    public void Render_ShouldDisplayMessage_WhenValidationMessagesIsEmpty()
    {
        // Arrange
        const string message = "This is the main message";
        var messages = Array.Empty<string>();

        // Act
        var cut = _ctx.Render<NebaAlert>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Error)
            .Add(p => p.Message, message)
            .Add(p => p.ValidationMessages, messages));

        // Assert
        cut.FindAll("ul.neba-alert-validation-list").Count.ShouldBe(0);
        cut.Markup.ShouldContain(message);
    }

    [Fact(DisplayName = "Should invoke OnCloseIconClicked callback when close button is clicked")]
    public async Task OnCloseIconClicked_ShouldInvokeCallback_WhenCloseButtonClicked()
    {
        // Arrange
        var callbackInvoked = false;

        var cut = _ctx.Render<NebaAlert>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Info)
            .Add(p => p.Message, "Test message")
            .Add(p => p.Dismissible, true)
            .Add(p => p.OnCloseIconClicked, EventCallback.Factory.Create(this, async () =>
            {
                callbackInvoked = true;
                await Task.CompletedTask;
            })));

        // Act
        var button = cut.Find("button.neba-alert-close");
        await button.ClickAsync(new());

        // Assert
        callbackInvoked.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should invoke OnDismiss callback when close button is clicked")]
    public async Task OnDismiss_ShouldInvokeCallback_WhenCloseButtonClicked()
    {
        // Arrange
        var callbackInvoked = false;

        var cut = _ctx.Render<NebaAlert>(parameters => parameters
            .Add(p => p.Severity, NotifySeverity.Info)
            .Add(p => p.Message, "Test message")
            .Add(p => p.Dismissible, true)
            .Add(p => p.OnDismiss, EventCallback.Factory.Create(this, async () =>
            {
                callbackInvoked = true;
                await Task.CompletedTask;
            })));

        // Act
        var button = cut.Find("button.neba-alert-close");
        await button.ClickAsync(new());

        // Assert
        callbackInvoked.ShouldBeTrue();
    }

    [Theory(DisplayName = "Should set correct ARIA role for severity")]
    [InlineData(NotifySeverity.Error, "alert", TestDisplayName = "Error has alert role")]
    [InlineData(NotifySeverity.Warning, "alert", TestDisplayName = "Warning has alert role")]
    [InlineData(NotifySeverity.Info, "status", TestDisplayName = "Info has status role")]
    [InlineData(NotifySeverity.Success, "status", TestDisplayName = "Success has status role")]
    [InlineData(NotifySeverity.Normal, "status", TestDisplayName = "Normal has status role")]
    public void Render_ShouldSetCorrectAriaRole(NotifySeverity severity, string expectedRole)
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaAlert>(parameters => parameters
            .Add(p => p.Severity, severity)
            .Add(p => p.Message, "Test message"));

        // Assert
        var alertDiv = cut.Find("div.neba-alert");
        alertDiv.GetAttribute("role").ShouldBe(expectedRole);
    }
}