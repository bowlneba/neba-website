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

    [Theory(DisplayName = "Sanitize linkifies bare URLs typed as plain text")]
    [InlineData("<p>Visit BowlNEBA.com/Tournaments for details.</p>", "<a href=\"https://BowlNEBA.com/Tournaments\">BowlNEBA.com/Tournaments</a>")]
    [InlineData("<p>Visit www.bowlneba.com for details.</p>", "<a href=\"https://www.bowlneba.com\">www.bowlneba.com</a>")]
    [InlineData("<p>Visit https://bowlneba.com/stats for details.</p>", "<a href=\"https://bowlneba.com/stats\">https://bowlneba.com/stats</a>")]
    public void Sanitize_ShouldLinkifyBareUrls(string html, string expectedAnchor)
    {
        // Act
        var result = HtmlContentSanitizer.Sanitize(html);

        // Assert
        result.ShouldContain(expectedAnchor);
    }

    [Fact(DisplayName = "Sanitize does not double-wrap a URL that is already a link")]
    public void Sanitize_ShouldNotDoubleWrapExistingLink()
    {
        // Arrange
        const string html = "<p><a href=\"https://bowlneba.com/tournaments\">bowlneba.com/tournaments</a></p>";

        // Act
        var result = HtmlContentSanitizer.Sanitize(html);

        // Assert
        result.ShouldContain("<a href=\"https://bowlneba.com/tournaments\">bowlneba.com/tournaments</a>");
        result.ShouldNotContain("<a href=\"https://bowlneba.com/tournaments\"><a href");
    }

    [Fact(DisplayName = "Sanitize excludes trailing sentence punctuation from a linkified URL")]
    public void Sanitize_ShouldExcludeTrailingPunctuationFromLinkifiedUrl()
    {
        // Arrange
        const string html = "<p>Visit BowlNEBA.com/Tournaments, then check stats.</p>";

        // Act
        var result = HtmlContentSanitizer.Sanitize(html);

        // Assert
        result.ShouldContain("<a href=\"https://BowlNEBA.com/Tournaments\">BowlNEBA.com/Tournaments</a>,");
    }
}