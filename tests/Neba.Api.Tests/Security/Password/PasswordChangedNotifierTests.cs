using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Email;
using Neba.Api.Security;
using Neba.Api.Security.Password;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Password;

[UnitTest]
[Component("Security")]
public sealed class PasswordChangedNotifierTests
{
    private readonly Mock<IEmailSender> _emailSender = new(MockBehavior.Strict);
    private readonly FakeLogger<PasswordChangedNotifier> _logger = new();

    private PasswordChangedNotifier CreateNotifier()
        => new(_emailSender.Object, new WebsiteSettings { BaseUrl = "https://bowlneba.com" }, _logger);

    [Fact(DisplayName = "NotifyAsync should email the user a password-changed notice")]
    public async Task NotifyAsync_ShouldSendNotice()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var user = ApplicationUserFactory.Create();
        EmailMessage? sent = null;
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), ct))
            .Callback<EmailMessage, CancellationToken>((message, _) => sent = message)
            .Returns(Task.CompletedTask);

        // Act
        await CreateNotifier().NotifyAsync(user, ct);

        // Assert
        sent.ShouldNotBeNull();
        sent.To.ShouldBe(user.Email);
        sent.Subject.ShouldBe("Your BowlNEBA password was changed");
        sent.HtmlBody.ShouldContain("https://bowlneba.com/account/forgot-password");
    }

    [Fact(DisplayName = "NotifyAsync should log an error and not throw when the email fails to send")]
    public async Task NotifyAsync_ShouldLogAndSwallow_WhenSendFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var user = ApplicationUserFactory.Create();
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), ct))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        // Act
        await CreateNotifier().NotifyAsync(user, ct);

        // Assert
        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Error);
        record.Exception.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact(DisplayName = "NotifyAsync should not swallow a cancelled send")]
    public async Task NotifyAsync_ShouldPropagate_WhenCancelled()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var user = ApplicationUserFactory.Create();
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), ct))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => CreateNotifier().NotifyAsync(user, ct));
    }
}
