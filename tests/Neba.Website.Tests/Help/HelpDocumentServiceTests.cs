using Neba.TestFactory.Attributes;
using Neba.Website.Server.Help;

namespace Neba.Website.Tests.Help;

[UnitTest]
[Component("Website.Help.HelpDocumentService")]
public sealed class HelpDocumentServiceTests
{
    private readonly HelpDocumentService _sut = new();

    [Fact(DisplayName = "Should return null when no doc is embedded for the given name")]
    public void GetRenderedHtml_ShouldReturnNull_WhenDocDoesNotExist()
    {
        // Act
        var html = _sut.GetRenderedHtml("no-such-doc");

        // Assert
        html.ShouldBeNull();
    }

    [Fact(DisplayName = "Should render the doc's heading as HTML")]
    public void GetRenderedHtml_ShouldRenderHeading_WhenDocExists()
    {
        // Act
        var html = _sut.GetRenderedHtml("create-sponsor");

        // Assert
        html.ShouldNotBeNull();
        html.ShouldContain(">Create a Sponsor</h1>");
    }

    [Fact(DisplayName = "Should rewrite doc-relative image sources to the /help/images endpoint")]
    public void GetRenderedHtml_ShouldRewriteImageSources_ToHelpImagesEndpoint()
    {
        // Act
        var html = _sut.GetRenderedHtml("create-sponsor");

        // Assert
        html.ShouldNotBeNull();
        html.ShouldContain("src=\"/help/images/create-sponsor/sponsors-list-fab.png\"");
    }

    [Fact(DisplayName = "Should render Markdown tables (Troubleshooting section) as HTML tables")]
    public void GetRenderedHtml_ShouldRenderMarkdownTables_AsHtmlTables()
    {
        // Act
        var html = _sut.GetRenderedHtml("create-sponsor");

        // Assert
        html.ShouldNotBeNull();
        html.ShouldContain("<table>");
    }

    [Fact(DisplayName = "Should render docs with no image references without error")]
    public void GetRenderedHtml_ShouldRenderDoc_WhenDocHasNoImages()
    {
        // Act
        var html = _sut.GetRenderedHtml("reset-password");

        // Assert
        html.ShouldNotBeNull();
        html.ShouldNotContain("/help/images/");
    }

    [Fact(DisplayName = "Should cache the rendered HTML across repeated calls for the same doc")]
    public void GetRenderedHtml_ShouldReturnSameInstance_OnRepeatedCallsForSameDoc()
    {
        // Act
        var first = _sut.GetRenderedHtml("create-sponsor");
        var second = _sut.GetRenderedHtml("create-sponsor");

        // Assert
        first.ShouldBeSameAs(second);
    }
}