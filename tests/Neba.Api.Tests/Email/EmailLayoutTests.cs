using Neba.Api.Email;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Email;

[UnitTest]
[Component("Email")]
public sealed class EmailLayoutTests
{
    [Fact(DisplayName = "Wrap should default the logo and footer link to the production domain when no website base URL is given")]
    public void Wrap_ShouldDefaultLogoAndFooterLinkToProductionDomain_WhenNoWebsiteBaseUrlGiven()
    {
        // Arrange & Act
        var html = EmailLayout.Wrap("<p>body</p>");

        // Assert
        html.ShouldContain("src=\"https://bowlneba.com/images/neba-logo.png\"");
        html.ShouldContain("href=\"https://bowlneba.com\"");
    }

    [Fact(DisplayName = "Wrap should use the given website base URL for the logo and footer link")]
    public void Wrap_ShouldUseGivenWebsiteBaseUrlForLogoAndFooterLink()
    {
        // Arrange
        const string websiteBaseUrl = "https://preview.bowlneba.com";

        // Act
        var html = EmailLayout.Wrap("<p>body</p>", websiteBaseUrl);

        // Assert
        html.ShouldContain($"src=\"{websiteBaseUrl}/images/neba-logo.png\"");
        html.ShouldContain($"href=\"{websiteBaseUrl}\"");
        html.ShouldNotContain("https://bowlneba.com/images/neba-logo.png");
    }

    [Fact(DisplayName = "Wrap should embed the given inner HTML body")]
    public void Wrap_ShouldEmbedGivenInnerHtmlBody()
    {
        // Arrange
        const string innerHtml = "<h1>Test Content</h1>";

        // Act
        var html = EmailLayout.Wrap(innerHtml);

        // Assert
        html.ShouldContain(innerHtml);
    }
}
