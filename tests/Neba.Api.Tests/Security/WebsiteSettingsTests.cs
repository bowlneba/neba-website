using Neba.Api.Security;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security;

[UnitTest]
[Component("Security")]
public sealed class WebsiteSettingsTests
{
    [Fact(DisplayName = "WithPreviewOverride should replace BaseUrl with the preview domain when InPreview is true")]
    public void WithPreviewOverride_ShouldReplaceBaseUrlWithPreviewDomain_WhenInPreviewIsTrue()
    {
        // Arrange
        var settings = new WebsiteSettings { InPreview = true, BaseUrl = "https://bowlneba.com" };

        // Act
        var result = settings.WithPreviewOverride();

        // Assert
        result.BaseUrl.ShouldBe("https://preview.bowlneba.com");
    }

    [Fact(DisplayName = "WithPreviewOverride should leave BaseUrl unchanged when InPreview is false")]
    public void WithPreviewOverride_ShouldLeaveBaseUrlUnchanged_WhenInPreviewIsFalse()
    {
        // Arrange
        var settings = new WebsiteSettings { InPreview = false, BaseUrl = "http://localhost:5200" };

        // Act
        var result = settings.WithPreviewOverride();

        // Assert
        result.BaseUrl.ShouldBe("http://localhost:5200");
    }
}
