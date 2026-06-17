using Neba.Api.Security.Emails;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.Emails;

[UnitTest]
[Component("Email")]
public sealed class ConfirmAccountEmailTests
{
    [Fact(DisplayName = "ToHtmlBody should contain the confirmation link")]
    public void ToHtmlBody_ShouldContainConfirmationLink()
    {
        // Arrange
        const string link = "https://bowlneba.com/confirm?token=abc123";
        var email = new ConfirmAccountEmail(link);

        // Act
        var html = email.ToHtmlBody();

        // Assert
        html.ShouldContain(link);
    }

    [Fact(DisplayName = "ToHtmlBody should HTML-encode ampersands in the confirmation link")]
    public void ToHtmlBody_ShouldHtmlEncodeAmpersandsInConfirmationLink()
    {
        // Arrange
        const string link = "https://bowlneba.com/confirm?userId=1&token=abc";
        var email = new ConfirmAccountEmail(link);

        // Act
        var html = email.ToHtmlBody();

        // Assert
        html.ShouldContain("userId=1&amp;token=abc");
        html.ShouldNotContain("userId=1&token=abc");
    }
}
