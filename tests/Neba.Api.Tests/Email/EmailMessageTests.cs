using Neba.Api.Email;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Email;

namespace Neba.Api.Tests.Email;

[UnitTest]
[Component("Email")]
public sealed class EmailMessageTests
{
    [Fact(DisplayName = "Should set and expose all required properties")]
    public void Create_ShouldExposeAllRequiredProperties()
    {
        // Arrange & Act
        var message = EmailMessageFactory.Create(
            to: "recipient@example.com",
            subject: "Test Subject",
            htmlBody: "<p>Hello</p>");

        // Assert
        message.To.ShouldBe("recipient@example.com");
        message.Subject.ShouldBe("Test Subject");
        message.HtmlBody.ShouldBe("<p>Hello</p>");
        message.ReplyTo.ShouldBeNull();
    }

    [Fact(DisplayName = "Should expose optional ReplyTo when provided")]
    public void Create_ShouldExposeReplyTo_WhenProvided()
    {
        // Arrange & Act
        var message = EmailMessageFactory.Create(replyTo: "support@example.com");

        // Assert
        message.ReplyTo.ShouldBe("support@example.com");
    }
}