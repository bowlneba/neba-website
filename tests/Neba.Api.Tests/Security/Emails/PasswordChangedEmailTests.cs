using Neba.Api.Security.Emails;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.Emails;

[UnitTest]
[Component("Email")]
public sealed class PasswordChangedEmailTests
{
    [Fact(DisplayName = "ToHtmlBody should tell the user the password was changed")]
    public void ToHtmlBody_ShouldStateThePasswordWasChanged()
    {
        // Arrange
        var email = new PasswordChangedEmail("https://www.bowlneba.com");

        // Act
        var html = email.ToHtmlBody();

        // Assert
        html.ShouldContain("Your password was changed");
    }

    [Fact(DisplayName = "ToHtmlBody should link to the forgot-password page on the website")]
    public void ToHtmlBody_ShouldLinkToForgotPasswordPage()
    {
        // Arrange
        var email = new PasswordChangedEmail("https://www.bowlneba.com");

        // Act
        var html = email.ToHtmlBody();

        // Assert
        html.ShouldContain("href=\"https://www.bowlneba.com/account/forgot-password\"");
    }

    [Fact(DisplayName = "ToHtmlBody should HTML-encode the forgot-password link")]
    public void ToHtmlBody_ShouldHtmlEncodeForgotPasswordLink()
    {
        // Arrange
        var email = new PasswordChangedEmail("https://www.bowlneba.com/?a=1&b=2");

        // Act
        var html = email.ToHtmlBody();

        // Assert
        html.ShouldContain("?a=1&amp;b=2/account/forgot-password");
        html.ShouldNotContain("?a=1&b=2/account/forgot-password");
    }
}
