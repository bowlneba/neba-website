using HtmlAgilityPack;

using Neba.Infrastructure.Documents;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.Documents;

[UnitTest]
[Component("Documents")]
public sealed class HtmlProcessorTests
{
    private readonly GoogleSettings _settings;
    private readonly HtmlProcessor _processor;

    public HtmlProcessorTests()
    {
        _settings = new GoogleSettings
        {
            ApplicationName = "Test Application",
            Credentials = new GoogleCredentials
            {
                ProjectId = "test-project",
                PrivateKey = "test-key",
                ClientEmail = "test@test.iam.gserviceaccount.com",
                PrivateKeyId = "test-key-id"
            },
            Documents =
            [
                new GoogleDocument
                {
                    Name = "bylaws",
                    DocumentId = "1ABC123",
                    WebRoute = "/about/bylaws"
                },
                new GoogleDocument
                {
                    Name = "tournament-rules",
                    DocumentId = "1DEF456",
                    WebRoute = "/tournaments/rules"
                }
            ]
        };

        _processor = new HtmlProcessor(_settings);
    }

    [Fact(DisplayName = "Process should extract body content from full HTML document")]
    public void Process_ExtractsBodyContent_WhenHtmlContainsBodyTag()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <head><title>Test</title></head>
            <body><h1>Test Document</h1><p>Content here</p></body>
            </html>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert
        result.ShouldContain("<h1");
        result.ShouldContain("Test Document");
        result.ShouldContain("<p>Content here</p>");
        result.ShouldNotContain("<html");
        result.ShouldNotContain("<head");
        result.ShouldNotContain("<title>");
    }

    [Fact(DisplayName = "Process should return raw HTML when no body tag exists")]
    public void Process_ReturnsRawHtml_WhenNoBodyTagExists()
    {
        // Arrange
        const string rawHtml = """
            <div>Content without body tag</div>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert
        result.ShouldBe(rawHtml);
    }

    [Fact(DisplayName = "Process should generate anchor IDs for all heading levels")]
    public void Process_GeneratesAnchorIds_ForAllHeadingLevels()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <body>
                <h1>Main Title</h1>
                <h2>Subtitle</h2>
                <h3>Section</h3>
                <h4>Subsection</h4>
                <h5>Detail</h5>
                <h6>Fine Print</h6>
            </body>
            </html>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert
        var doc = new HtmlDocument();
        doc.LoadHtml(result);

        doc.DocumentNode.SelectSingleNode("//h1")?.GetAttributeValue("id", "").ShouldBe("main-title");
        doc.DocumentNode.SelectSingleNode("//h2")?.GetAttributeValue("id", "").ShouldBe("subtitle");
        doc.DocumentNode.SelectSingleNode("//h3")?.GetAttributeValue("id", "").ShouldBe("section");
        doc.DocumentNode.SelectSingleNode("//h4")?.GetAttributeValue("id", "").ShouldBe("subsection");
        doc.DocumentNode.SelectSingleNode("//h5")?.GetAttributeValue("id", "").ShouldBe("detail");
        doc.DocumentNode.SelectSingleNode("//h6")?.GetAttributeValue("id", "").ShouldBe("fine-print");
    }

    [Fact(DisplayName = "Process should generate kebab-case anchor IDs from heading text")]
    public void Process_GeneratesKebabCaseAnchorIds_FromHeadingText()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <body>
                <h1>Section 10.3 - Hall of Fame</h1>
            </body>
            </html>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert
        var doc = new HtmlDocument();
        doc.LoadHtml(result);

        doc.DocumentNode.SelectSingleNode("//h1")?.GetAttributeValue("id", "")
            .ShouldBe("section-10.3-hall-of-fame");
    }

    [Fact(DisplayName = "Process should handle headings with special characters")]
    public void Process_HandlesSpecialCharacters_InHeadingText()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <body>
                <h1>Officers & Board Members</h1>
                <h2>President's Cup (2024)</h2>
                <h3>Article #1: Name & Purpose</h3>
            </body>
            </html>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert
        var doc = new HtmlDocument();
        doc.LoadHtml(result);

        doc.DocumentNode.SelectSingleNode("//h1")?.GetAttributeValue("id", "")
            .ShouldBe("officers-board-members");
        doc.DocumentNode.SelectSingleNode("//h2")?.GetAttributeValue("id", "")
            .ShouldBe("president-s-cup-2024");
        doc.DocumentNode.SelectSingleNode("//h3")?.GetAttributeValue("id", "")
            .ShouldBe("article-1-name-purpose");
    }

    [Fact(DisplayName = "Process should handle headings with consecutive spaces")]
    public void Process_HandlesConsecutiveSpaces_InHeadingText()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <body>
                <h1>Multiple    Spaces    Here</h1>
            </body>
            </html>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert
        var doc = new HtmlDocument();
        doc.LoadHtml(result);

        doc.DocumentNode.SelectSingleNode("//h1")?.GetAttributeValue("id", "")
            .ShouldBe("multiple-spaces-here");
    }

    [Fact(DisplayName = "Process should replace Google Docs URLs with internal routes")]
    public void Process_ReplacesGoogleDocsUrls_WithInternalRoutes()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <body>
                <p>See the <a href="https://docs.google.com/document/d/1ABC123/edit">Bylaws</a> for details.</p>
                <p>Check the <a href="https://docs.google.com/document/d/1DEF456/view">Tournament Rules</a>.</p>
            </body>
            </html>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert
        var doc = new HtmlDocument();
        doc.LoadHtml(result);

        var links = doc.DocumentNode.SelectNodes("//a[@href]");
        links.Count.ShouldBe(2);
        links[0].GetAttributeValue("href", "").ShouldBe("/about/bylaws");
        links[1].GetAttributeValue("href", "").ShouldBe("/tournaments/rules");
    }

    [Fact(DisplayName = "Process should preserve anchors when replacing Google Docs URLs")]
    public void Process_PreservesAnchors_WhenReplacingGoogleDocsUrls()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <body>
                <p>See <a href="https://docs.google.com/document/d/1ABC123/edit#heading=h.abc123">Section 5</a>.</p>
            </body>
            </html>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert
        var doc = new HtmlDocument();
        doc.LoadHtml(result);

        var link = doc.DocumentNode.SelectSingleNode("//a[@href]");
        link.ShouldNotBeNull();
        link!.GetAttributeValue("href", "").ShouldBe("/about/bylaws#heading=h.abc123");
    }

    [Fact(DisplayName = "Process should not modify non-Google Docs links")]
    public void Process_DoesNotModify_NonGoogleDocsLinks()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <body>
                <p>Visit our <a href="https://example.com">website</a>.</p>
                <p>See <a href="/internal/page">this page</a>.</p>
                <p>Email us at <a href="mailto:info@example.com">info@example.com</a>.</p>
            </body>
            </html>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert
        var doc = new HtmlDocument();
        doc.LoadHtml(result);

        var links = doc.DocumentNode.SelectNodes("//a[@href]");
        links.Count.ShouldBe(3);
        links[0].GetAttributeValue("href", "").ShouldBe("https://example.com");
        links[1].GetAttributeValue("href", "").ShouldBe("/internal/page");
        links[2].GetAttributeValue("href", "").ShouldBe("mailto:info@example.com");
    }

    [Fact(DisplayName = "Process should not replace Google Docs URLs for unknown documents")]
    public void Process_DoesNotReplace_UnknownGoogleDocsUrls()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <body>
                <p>See <a href="https://docs.google.com/document/d/1UNKNOWN999/edit">this document</a>.</p>
            </body>
            </html>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert
        var doc = new HtmlDocument();
        doc.LoadHtml(result);

        var link = doc.DocumentNode.SelectSingleNode("//a[@href]");
        link.ShouldNotBeNull();
        link!.GetAttributeValue("href", "").ShouldBe("https://docs.google.com/document/d/1UNKNOWN999/edit");
    }

    [Fact(DisplayName = "Process should handle HTML without any links")]
    public void Process_HandlesHtml_WithoutLinks()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <body>
                <h1>Title</h1>
                <p>Content without any links.</p>
            </body>
            </html>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert
        result.ShouldContain("Title");
        result.ShouldContain("Content without any links.");
    }

    [Fact(DisplayName = "Process should handle HTML without any headings")]
    public void Process_HandlesHtml_WithoutHeadings()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <body>
                <p>Just a paragraph.</p>
                <div>Just a div.</div>
            </body>
            </html>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert
        result.ShouldContain("Just a paragraph.");
        result.ShouldContain("Just a div.");
    }

    [Fact(DisplayName = "Process should handle empty body tag")]
    public void Process_HandlesEmpty_BodyTag()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <body></body>
            </html>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Process should handle links without href attribute")]
    public void Process_HandlesLinks_WithoutHrefAttribute()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <body>
                <a>Link without href</a>
                <a href="">Link with empty href</a>
            </body>
            </html>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert - Should not throw and should process successfully
        result.ShouldContain("Link without href");
        result.ShouldContain("Link with empty href");
    }

    [Fact(DisplayName = "Process should perform all transformations in correct order")]
    public void Process_PerformsAllTransformations_InCorrectOrder()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <head><title>Test</title></head>
            <body>
                <h1>Section 1</h1>
                <p>See <a href="https://docs.google.com/document/d/1ABC123/edit#heading=h.xyz">Bylaws Section 2</a>.</p>
                <h2>Section 2</h2>
                <p>Check <a href="https://example.com">external link</a>.</p>
            </body>
            </html>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert
        var doc = new HtmlDocument();
        doc.LoadHtml(result);

        // Body extracted
        result.ShouldNotContain("<html");
        result.ShouldNotContain("<head");

        // Anchor IDs generated
        doc.DocumentNode.SelectSingleNode("//h1")?.GetAttributeValue("id", "").ShouldBe("section-1");
        doc.DocumentNode.SelectSingleNode("//h2")?.GetAttributeValue("id", "").ShouldBe("section-2");

        // Google Docs link replaced with anchor preserved
        var googleLink = doc.DocumentNode.SelectSingleNode("//a[contains(text(), 'Bylaws')]");
        googleLink.ShouldNotBeNull();
        googleLink!.GetAttributeValue("href", "").ShouldBe("/about/bylaws#heading=h.xyz");

        // External link preserved
        var externalLink = doc.DocumentNode.SelectSingleNode("//a[contains(text(), 'external')]");
        externalLink.ShouldNotBeNull();
        externalLink!.GetAttributeValue("href", "").ShouldBe("https://example.com");
    }

    [Fact(DisplayName = "Process should handle malformed HTML gracefully")]
    public void Process_HandlesMalformedHtml_Gracefully()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <body>
                <h1>Unclosed heading
                <p>Unclosed paragraph
                <div><span>Nested but unclosed
            </body>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert - Should not throw
        result.ShouldContain("Unclosed heading");
        result.ShouldContain("Unclosed paragraph");
    }

    [Fact(DisplayName = "Process should handle headings with only special characters")]
    public void Process_HandlesHeadings_WithOnlySpecialCharacters()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <body>
                <h1>***</h1>
                <h2>---</h2>
            </body>
            </html>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert
        var doc = new HtmlDocument();
        doc.LoadHtml(result);

        // Should generate empty or minimal IDs (edge case)
        var h1Id = doc.DocumentNode.SelectSingleNode("//h1")?.GetAttributeValue("id", "");
        var h2Id = doc.DocumentNode.SelectSingleNode("//h2")?.GetAttributeValue("id", "");

        h1Id.ShouldNotBeNull();
        h2Id.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Process should preserve periods in anchor IDs")]
    public void Process_PreservesPeriods_InAnchorIds()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <body>
                <h1>Section 10.3.1</h1>
            </body>
            </html>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert
        var doc = new HtmlDocument();
        doc.LoadHtml(result);

        doc.DocumentNode.SelectSingleNode("//h1")?.GetAttributeValue("id", "")
            .ShouldBe("section-10.3.1");
    }

    [Fact(DisplayName = "Process should handle multiple Google Docs links to same document")]
    public void Process_HandlesMultipleLinks_ToSameDocument()
    {
        // Arrange
        const string rawHtml = """
            <html>
            <body>
                <p><a href="https://docs.google.com/document/d/1ABC123/edit">Bylaws</a></p>
                <p><a href="https://docs.google.com/document/d/1ABC123/view">Same Bylaws</a></p>
                <p><a href="https://docs.google.com/document/d/1ABC123/preview">Bylaws Again</a></p>
            </body>
            </html>
            """;

        // Act
        var result = _processor.Process(rawHtml);

        // Assert
        var doc = new HtmlDocument();
        doc.LoadHtml(result);

        var links = doc.DocumentNode.SelectNodes("//a[@href]");
        links.Count.ShouldBe(3);
        links.ShouldAllBe(link => link.GetAttributeValue("href", "") == "/about/bylaws");
    }
}