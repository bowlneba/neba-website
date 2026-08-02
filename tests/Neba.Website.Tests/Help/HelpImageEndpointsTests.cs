using Microsoft.AspNetCore.Http.HttpResults;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Help;

namespace Neba.Website.Tests.Help;

[UnitTest]
[Component("Website.Help.HelpImageEndpoints")]
public sealed class HelpImageEndpointsTests
{
    [Fact(DisplayName = "Should return NotFound when no embedded image matches the doc/file")]
    public void GetImage_ShouldReturnNotFound_WhenResourceDoesNotExist()
    {
        // Act
        var result = HelpImageEndpoints.GetImage("no-such-doc", "no-such-file.png");

        // Assert
        result.ShouldBeOfType<NotFound>();
    }

    [Fact(DisplayName = "Should return the embedded image with the matching content type when it exists")]
    public void GetImage_ShouldReturnFileWithPngContentType_WhenResourceExists()
    {
        // Act
        var result = HelpImageEndpoints.GetImage("create-sponsor", "sponsors-list-fab.png");

        // Assert
        var fileResult = result.ShouldBeOfType<FileStreamHttpResult>();
        fileResult.ContentType.ShouldBe("image/png");
    }

    [Theory(DisplayName = "Should map known file extensions to their content type")]
    [InlineData("screenshot.png", "image/png")]
    [InlineData("SCREENSHOT.PNG", "image/png")]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("photo.JPG", "image/jpeg")]
    [InlineData("animation.gif", "image/gif")]
    [InlineData("icon.svg", "image/svg+xml")]
    [InlineData("document.pdf", "application/octet-stream")]
    [InlineData("no-extension", "application/octet-stream")]
    public void GetContentType_ShouldReturnExpectedMimeType_ForFileName(string fileName, string expectedContentType)
    {
        // Act
        var contentType = HelpImageEndpoints.GetContentType(fileName);

        // Assert
        contentType.ShouldBe(expectedContentType);
    }
}