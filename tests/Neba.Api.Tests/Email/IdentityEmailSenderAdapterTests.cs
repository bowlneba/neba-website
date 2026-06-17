using Neba.Api.Email;
using Neba.Api.Security.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Email;

namespace Neba.Api.Tests.Email;

[UnitTest]
[Component("Email")]
public sealed class IdentityEmailSenderAdapterTests
{
    private readonly Mock<IEmailSender> _sender;
    private readonly IdentityEmailSenderAdapter _sut;
    private readonly ApplicationUser _user = new();

    public IdentityEmailSenderAdapterTests()
    {
        _sender = new Mock<IEmailSender>(MockBehavior.Strict);
        _sut = new IdentityEmailSenderAdapter(_sender.Object);
    }

    [Fact(DisplayName = "SendConfirmationLinkAsync should send email with confirmation link")]
    public async Task SendConfirmationLinkAsync_ShouldSendEmail_WithConfirmationLink()
    {
        // Arrange
        const string email = EmailMessageFactory.ValidTo;
        const string link = "https://example.com/confirm?token=abc";
        _sender
            .Setup(s => s.SendAsync(
                It.Is<EmailMessage>(m =>
                    m.To == email &&
                    m.Subject == "Confirm your BowlNEBA Account" &&
                    m.HtmlBody.Contains(link)),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SendConfirmationLinkAsync(_user, email, link);

        // Assert
        _sender.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), CancellationToken.None), Times.Once);
    }

    [Fact(DisplayName = "SendPasswordResetCodeAsync should send email with reset code")]
    public async Task SendPasswordResetCodeAsync_ShouldSendEmail_WithResetCode()
    {
        // Arrange
        const string email = EmailMessageFactory.ValidTo;
        const string code = "RESET123";
        _sender
            .Setup(s => s.SendAsync(
                It.Is<EmailMessage>(m =>
                    m.To == email &&
                    m.Subject == "Reset your BowlNEBA Password" &&
                    m.HtmlBody.Contains(code)),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SendPasswordResetCodeAsync(_user, email, code);

        // Assert
        _sender.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), CancellationToken.None), Times.Once);
    }

    [Fact(DisplayName = "SendPasswordResetLinkAsync should send email with reset link")]
    public async Task SendPasswordResetLinkAsync_ShouldSendEmail_WithResetLink()
    {
        // Arrange
        const string email = EmailMessageFactory.ValidTo;
        const string link = "https://example.com/reset?token=xyz";
        _sender
            .Setup(s => s.SendAsync(
                It.Is<EmailMessage>(m =>
                    m.To == email &&
                    m.Subject == "Reset your BowlNEBA Password" &&
                    m.HtmlBody.Contains(link)),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SendPasswordResetLinkAsync(_user, email, link);

        // Assert
        _sender.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), CancellationToken.None), Times.Once);
    }
}
