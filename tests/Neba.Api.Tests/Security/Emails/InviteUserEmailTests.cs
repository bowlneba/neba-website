using Neba.Api.Security.Emails;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.Emails;

[UnitTest]
[Component("Email")]
public sealed class InviteUserEmailTests
{
    [Fact(DisplayName = "ToHtmlBody should contain the invite link")]
    public void ToHtmlBody_ShouldContainInviteLink()
    {
        // Arrange
        const string link = "https://bowlneba.com/set-password?token=xyz789";
        var email = new InviteUserEmail(link);

        // Act
        var html = email.ToHtmlBody();

        // Assert
        html.ShouldContain(link);
    }

    [Fact(DisplayName = "ToHtmlBody should HTML-encode ampersands in the invite link")]
    public void ToHtmlBody_ShouldHtmlEncodeAmpersandsInInviteLink()
    {
        // Arrange
        const string link = "https://bowlneba.com/set-password?userId=2&token=xyz";
        var email = new InviteUserEmail(link);

        // Act
        var html = email.ToHtmlBody();

        // Assert
        html.ShouldContain("userId=2&amp;token=xyz");
        html.ShouldNotContain("userId=2&token=xyz");
    }

    [Fact(DisplayName = "ToHtmlBody should use the invite link's own domain for the logo and footer link")]
    public void ToHtmlBody_ShouldUseInviteLinkOwnDomainForLogoAndFooterLink()
    {
        // Arrange
        const string link = "https://preview.bowlneba.com/set-password?token=xyz789";
        var email = new InviteUserEmail(link);

        // Act
        var html = email.ToHtmlBody();

        // Assert
        html.ShouldContain("src=\"https://preview.bowlneba.com/images/neba-logo.png\"");
        html.ShouldNotContain("https://bowlneba.com/images/neba-logo.png");
    }
}
