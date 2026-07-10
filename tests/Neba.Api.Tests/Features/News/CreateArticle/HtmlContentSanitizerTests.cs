using Neba.Api.Features.News.CreateArticle;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.News.CreateArticle;

[UnitTest]
[Component("News")]
public sealed class HtmlContentSanitizerTests
{
    [Fact(DisplayName = "Sanitize removes script tags")]
    public void Sanitize_ShouldRemoveScriptTags()
    {
        // Arrange
        const string html = "<p>Hello</p><script>alert('xss')</script>";

        // Act
        var result = HtmlContentSanitizer.Sanitize(html);

        // Assert
        result.ShouldNotContain("<script", Case.Insensitive);
        result.ShouldNotContain("alert");
    }

    [Fact(DisplayName = "Sanitize removes inline event handler attributes")]
    public void Sanitize_ShouldRemoveEventHandlerAttributes()
    {
        // Arrange
        const string html = "<p onclick=\"alert('xss')\">Hello</p>";

        // Act
        var result = HtmlContentSanitizer.Sanitize(html);

        // Assert
        result.ShouldNotContain("onclick", Case.Insensitive);
    }

    [Fact(DisplayName = "Sanitize removes javascript: URI schemes from links")]
    public void Sanitize_ShouldRemoveJavascriptUriScheme()
    {
        // Arrange
        const string html = "<a href=\"javascript:alert('xss')\">Click</a>";

        // Act
        var result = HtmlContentSanitizer.Sanitize(html);

        // Assert
        result.ShouldNotContain("javascript:", Case.Insensitive);
    }

    [Fact(DisplayName = "Sanitize removes data: URI schemes from images")]
    public void Sanitize_ShouldRemoveDataUriScheme()
    {
        // Arrange
        const string html = "<img src=\"data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==\" />";

        // Act
        var result = HtmlContentSanitizer.Sanitize(html);

        // Assert
        result.ShouldNotContain("data:", Case.Insensitive);
    }

    [Theory(DisplayName = "Sanitize removes non-allowlisted tags")]
    [InlineData("<iframe src=\"https://example.com\"></iframe>", "<iframe")]
    [InlineData("<object data=\"https://example.com\"></object>", "<object")]
    [InlineData("<embed src=\"https://example.com\" />", "<embed")]
    public void Sanitize_ShouldRemoveNonAllowlistedTags(string html, string disallowedTag)
    {
        // Act
        var result = HtmlContentSanitizer.Sanitize(html);

        // Assert
        result.ShouldNotContain(disallowedTag, Case.Insensitive);
    }

    [Fact(DisplayName = "Sanitize preserves allowlisted formatting tags")]
    public void Sanitize_ShouldPreserveAllowlistedFormattingTags()
    {
        // Arrange
        const string html = "<p><strong>Bold</strong> and <em>italic</em> text.</p>";

        // Act
        var result = HtmlContentSanitizer.Sanitize(html);

        // Assert
        result.ShouldContain("<strong>Bold</strong>");
        result.ShouldContain("<em>italic</em>");
    }

    [Fact(DisplayName = "Sanitize preserves http(s) URI schemes on links")]
    public void Sanitize_ShouldPreserveHttpUriScheme()
    {
        // Arrange
        const string html = "<a href=\"https://example.com\">Link</a>";

        // Act
        var result = HtmlContentSanitizer.Sanitize(html);

        // Assert
        result.ShouldContain("https://example.com");
    }

    [Fact(DisplayName = "Sanitize preserves mailto URI schemes on links")]
    public void Sanitize_ShouldPreserveMailtoUriScheme()
    {
        // Arrange
        const string html = "<a href=\"mailto:someone@example.com\">Email</a>";

        // Act
        var result = HtmlContentSanitizer.Sanitize(html);

        // Assert
        result.ShouldContain("mailto:someone@example.com");
    }

    [Fact(DisplayName = "Sanitize returns an empty string when input contains only disallowed content")]
    public void Sanitize_ShouldReturnEmptyString_WhenInputContainsOnlyDisallowedContent()
    {
        // Arrange
        const string html = "<script>alert('xss')</script>";

        // Act
        var result = HtmlContentSanitizer.Sanitize(html);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Sanitize returns an empty string when input is empty")]
    public void Sanitize_ShouldReturnEmptyString_WhenInputIsEmpty()
    {
        // Act
        var result = HtmlContentSanitizer.Sanitize(string.Empty);

        // Assert
        result.ShouldBeEmpty();
    }
}