using Neba.Api.Security.Emails;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.Emails;

[UnitTest]
[Component("Email")]
public sealed class AdminResetPasswordEmailTests
{
    [Fact(DisplayName = "ToHtmlBody should contain the temporary password")]
    public void ToHtmlBody_ShouldContainTempPassword()
    {
        // Arrange
        const string tempPassword = "Abc123Xyz9";
        var email = new AdminResetPasswordEmail(tempPassword);

        // Act
        var html = email.ToHtmlBody();

        // Assert
        html.ShouldContain(tempPassword);
    }

    [Fact(DisplayName = "ToHtmlBody should HTML-encode special characters in the temporary password")]
    public void ToHtmlBody_ShouldHtmlEncodeSpecialCharactersInTempPassword()
    {
        // Arrange
        const string tempPassword = "<script>9";
        var email = new AdminResetPasswordEmail(tempPassword);

        // Act
        var html = email.ToHtmlBody();

        // Assert
        html.ShouldContain("&lt;script&gt;9");
        html.ShouldNotContain("<script>9");
    }
}
