using Neba.Api.Security.Emails;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.Emails;

[UnitTest]
[Component("Email")]
public sealed class ResetPasswordCodeEmailTests
{
    [Fact(DisplayName = "ToHtmlBody should contain the reset code")]
    public void ToHtmlBody_ShouldContainResetCode()
    {
        // Arrange
        const string code = "A1B2C3";
        var email = new ResetPasswordCodeEmail(code);

        // Act
        var html = email.ToHtmlBody();

        // Assert
        html.ShouldContain(code);
    }

    [Fact(DisplayName = "ToHtmlBody should HTML-encode special characters in the reset code")]
    public void ToHtmlBody_ShouldHtmlEncodeSpecialCharactersInResetCode()
    {
        // Arrange
        const string code = "<script>";
        var email = new ResetPasswordCodeEmail(code);

        // Act
        var html = email.ToHtmlBody();

        // Assert
        html.ShouldContain("&lt;script&gt;");
        html.ShouldNotContain("<script>");
    }
}