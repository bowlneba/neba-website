using Neba.Api.Security.Emails;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.Emails;

[UnitTest]
[Component("Email")]
public sealed class ResetPasswordLinkEmailTests
{
    [Fact(DisplayName = "ToHtmlBody should contain the reset link")]
    public void ToHtmlBody_ShouldContainResetLink()
    {
        // Arrange
        const string link = "https://bowlneba.com/reset-password?token=xyz789";
        var email = new ResetPasswordLinkEmail(link);

        // Act
        var html = email.ToHtmlBody();

        // Assert
        html.ShouldContain(link);
    }

    [Fact(DisplayName = "ToHtmlBody should HTML-encode ampersands in the reset link")]
    public void ToHtmlBody_ShouldHtmlEncodeAmpersandsInResetLink()
    {
        // Arrange
        const string link = "https://bowlneba.com/reset-password?userId=2&token=xyz";
        var email = new ResetPasswordLinkEmail(link);

        // Act
        var html = email.ToHtmlBody();

        // Assert
        html.ShouldContain("userId=2&amp;token=xyz");
        html.ShouldNotContain("userId=2&token=xyz");
    }
}