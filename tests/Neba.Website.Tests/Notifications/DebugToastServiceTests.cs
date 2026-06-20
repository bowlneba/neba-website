using Neba.TestFactory.Attributes;
using Neba.Website.Server.Notifications;

namespace Neba.Website.Tests.Notifications;

[UnitTest]
[Component("Website.Notifications.DebugToastService")]
public sealed class DebugToastServiceTests
{
    [Fact(DisplayName = "Show should set Current to the provided notification")]
    public void Show_ShouldSetCurrent_ToProvidedNotification()
    {
        // Arrange
        using var sut = new DebugToastService();

        // Act
        sut.Show("Alert", "Something happened", NotifySeverity.Warning);

        // Assert
        sut.Current.ShouldNotBeNull();
        sut.Current.Title.ShouldBe("Alert");
        sut.Current.Message.ShouldBe("Something happened");
        sut.Current.Severity.ShouldBe(NotifySeverity.Warning);
    }

    [Fact(DisplayName = "Show should fire OnChange event")]
    public void Show_ShouldFireOnChange()
    {
        // Arrange
        using var sut = new DebugToastService();
        var fired = 0;
        sut.OnChange += () => fired++;

        // Act
        sut.Show("Title", "Message", NotifySeverity.Info);

        // Assert
        fired.ShouldBe(1);
    }

    [Fact(DisplayName = "Show should not throw when no OnChange subscribers are registered")]
    public void Show_ShouldNotThrow_WhenNoOnChangeSubscribers()
    {
        // Arrange
        using var sut = new DebugToastService();

        // Act & Assert
        Should.NotThrow(() => sut.Show("Title", "Message", NotifySeverity.Info));
    }

    [Fact(DisplayName = "Show should cancel and replace the previous notification")]
    public void Show_ShouldReplaceExistingNotification()
    {
        // Arrange
        using var sut = new DebugToastService();
        sut.Show("First", "First message", NotifySeverity.Info);

        // Act
        sut.Show("Second", "Second message", NotifySeverity.Error);

        // Assert
        sut.Current.ShouldNotBeNull();
        sut.Current.Title.ShouldBe("Second");
    }

    [Fact(DisplayName = "Dismiss should clear Current")]
    public void Dismiss_ShouldClearCurrent()
    {
        // Arrange
        using var sut = new DebugToastService();
        sut.Show("Title", "Message", NotifySeverity.Success);

        // Act
        sut.Dismiss();

        // Assert
        sut.Current.ShouldBeNull();
    }

    [Fact(DisplayName = "Dismiss should fire OnChange event")]
    public void Dismiss_ShouldFireOnChange()
    {
        // Arrange
        using var sut = new DebugToastService();
        sut.Show("Title", "Message", NotifySeverity.Success);
        var fired = 0;
        sut.OnChange += () => fired++;

        // Act
        sut.Dismiss();

        // Assert
        fired.ShouldBe(1);
    }

    [Fact(DisplayName = "Dismiss should not throw when no notification is active")]
    public void Dismiss_ShouldNotThrow_WhenNothingIsShown()
    {
        // Arrange
        using var sut = new DebugToastService();

        // Act & Assert
        Should.NotThrow(() => sut.Dismiss());
    }

    [Fact(DisplayName = "Dismiss should not throw when called after a previous dismiss")]
    public void Dismiss_ShouldNotThrow_WhenCalledAfterDismiss()
    {
        // Arrange
        using var sut = new DebugToastService();
        sut.Show("Title", "Message", NotifySeverity.Info);
        sut.Dismiss();

        // Act & Assert
        Should.NotThrow(() => sut.Dismiss());
    }

    [Fact(DisplayName = "Dispose should leave Current null when no notification was active")]
    public void Dispose_ShouldLeaveCurrent_Null_WhenIdle()
    {
        // Arrange
        using var sut = new DebugToastService();

        // Act
        sut.Dispose();

        // Assert
        sut.Current.ShouldBeNull();
    }

    [Fact(DisplayName = "Dispose should not clear Current when a notification is active")]
    public void Dispose_ShouldNotClearCurrent_WhenNotificationIsActive()
    {
        // Arrange
        using var sut = new DebugToastService();
        sut.Show("Title", "Message", NotifySeverity.Warning);

        // Act
        sut.Dispose();

        // Assert — Dispose cancels the auto-dismiss timer but does not clear Current
        sut.Current.ShouldNotBeNull();
    }
}