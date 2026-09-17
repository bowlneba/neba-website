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

    [Fact(DisplayName = "ToHtmlBody should use the confirmation link's own domain for the logo and footer link")]
    public void ToHtmlBody_ShouldUseConfirmationLinkOwnDomainForLogoAndFooterLink()
    {
        // Arrange
        const string link = "https://preview.bowlneba.com/confirm?token=abc123";
        var email = new ConfirmAccountEmail(link);

        // Act
        var html = email.ToHtmlBody();

        // Assert
        html.ShouldContain("src=\"https://preview.bowlneba.com/images/neba-logo.png\"");
        html.ShouldNotContain("https://bowlneba.com/images/neba-logo.png");
    }
}