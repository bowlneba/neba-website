using Bunit;

using Microsoft.AspNetCore.Components;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;
using Neba.Website.Server.Documents;
using Neba.Website.Server.Notifications;

namespace Neba.Website.Tests.Documents;

[UnitTest]
[Component("Website.Documents.NebaDocument")]
public sealed class NebaDocumentTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render loading indicator when IsLoading is true")]
    public void Render_ShouldShowLoadingIndicator_WhenIsLoadingIsTrue()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Content, null));

        // Assert
        var loadingIndicator = cut.FindComponent<NebaLoadingIndicator>();
        loadingIndicator.ShouldNotBeNull();
        loadingIndicator.Instance.IsVisible.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should render custom loading text")]
    public void Render_ShouldShowCustomLoadingText_WhenProvided()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.LoadingText, "Custom loading text...")
            .Add(p => p.Content, null));

        // Assert
        var loadingIndicator = cut.FindComponent<NebaLoadingIndicator>();
        loadingIndicator.Instance.Text.ShouldBe("Custom loading text...");
    }

    [Fact(DisplayName = "Should use default loading text")]
    public void Render_ShouldUseDefaultLoadingText_WhenNotProvided()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Content, null));

        // Assert
        var loadingIndicator = cut.FindComponent<NebaLoadingIndicator>();
        loadingIndicator.Instance.Text.ShouldBe("Loading document...");
    }

    [Fact(DisplayName = "Should render error alert when ErrorMessage is provided")]
    public void Render_ShouldShowErrorAlert_WhenErrorMessageIsProvided()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.ErrorMessage, "Failed to load document")
            .Add(p => p.Content, null));

        // Assert
        var alert = cut.FindComponent<NebaAlert>();
        alert.ShouldNotBeNull();
        alert.Instance.Severity.ShouldBe(NotifySeverity.Error);
        alert.Instance.Message.ShouldBe("Failed to load document");
    }

    [Fact(DisplayName = "Should render custom error title")]
    public void Render_ShouldShowCustomErrorTitle_WhenProvided()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.ErrorMessage, "Something went wrong")
            .Add(p => p.ErrorTitle, "Custom Error Title")
            .Add(p => p.Content, null));

        // Assert
        var alert = cut.FindComponent<NebaAlert>();
        alert.Instance.Title.ShouldBe("Custom Error Title");
    }

    [Fact(DisplayName = "Should use default error title")]
    public void Render_ShouldUseDefaultErrorTitle_WhenNotProvided()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.ErrorMessage, "Something went wrong")
            .Add(p => p.Content, null));

        // Assert
        var alert = cut.FindComponent<NebaAlert>();
        alert.Instance.Title.ShouldBe("Error Loading Document");
    }

    [Fact(DisplayName = "Should make error alert dismissible")]
    public void Render_ShouldMakeErrorAlertDismissible()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.ErrorMessage, "Error message")
            .Add(p => p.Content, null));

        // Assert
        var alert = cut.FindComponent<NebaAlert>();
        alert.Instance.Dismissible.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should not render error alert when ErrorMessage is null")]
    public void Render_ShouldNotShowErrorAlert_WhenErrorMessageIsNull()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.ErrorMessage, null)
            .Add(p => p.Content, new MarkupString("<p>Content</p>")));

        // Assert
        cut.FindComponents<NebaAlert>().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should not render error alert when ErrorMessage is whitespace")]
    public void Render_ShouldNotShowErrorAlert_WhenErrorMessageIsWhitespace()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.ErrorMessage, "   ")
            .Add(p => p.Content, new MarkupString("<p>Content</p>")));

        // Assert
        cut.FindComponents<NebaAlert>().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render content when not loading and content has value")]
    public void Render_ShouldShowContent_WhenNotLoadingAndContentHasValue()
    {
        // Arrange
        var content = new MarkupString("<h1>Test Heading</h1><p>Test content</p>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.IsLoading, false)
            .Add(p => p.Content, content));

        // Assert
        var contentElement = cut.Find(".neba-document-content");
        contentElement.InnerHtml.ShouldContain("Test Heading");
        contentElement.InnerHtml.ShouldContain("Test content");
    }

    [Fact(DisplayName = "Should not render content when IsLoading is true")]
    public void Render_ShouldNotShowContent_WhenIsLoadingIsTrue()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Content, content));

        // Assert
        cut.FindAll(".neba-document-content").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should not render content when Content is null")]
    public void Render_ShouldNotShowContent_WhenContentIsNull()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.IsLoading, false)
            .Add(p => p.Content, null));

        // Assert
        cut.FindAll(".neba-document-content").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render table of contents by default")]
    public void Render_ShouldShowToc_WhenShowTableOfContentsIsDefault()
    {
        // Arrange
        var content = new MarkupString("<h1>Heading</h1>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.IsLoading, false)
            .Add(p => p.Content, content));

        // Assert
        var tocElement = cut.Find(".neba-document-toc");
        tocElement.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should not render table of contents when ShowTableOfContents is false")]
    public void Render_ShouldHideToc_WhenShowTableOfContentsIsFalse()
    {
        // Arrange
        var content = new MarkupString("<h1>Heading</h1>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.IsLoading, false)
            .Add(p => p.Content, content)
            .Add(p => p.ShowTableOfContents, false));

        // Assert
        cut.FindAll(".neba-document-toc").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render custom table of contents title")]
    public void Render_ShouldShowCustomTocTitle_WhenProvided()
    {
        // Arrange
        var content = new MarkupString("<h1>Heading</h1>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.IsLoading, false)
            .Add(p => p.Content, content)
            .Add(p => p.TableOfContentsTitle, "Custom TOC Title"));

        // Assert
        var tocTitle = cut.Find(".toc-title");
        tocTitle.TextContent.ShouldBe("Custom TOC Title");
    }

    [Fact(DisplayName = "Should use default table of contents title")]
    public void Render_ShouldUseDefaultTocTitle_WhenNotProvided()
    {
        // Arrange
        var content = new MarkupString("<h1>Heading</h1>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.IsLoading, false)
            .Add(p => p.Content, content));

        // Assert
        var tocTitle = cut.Find(".toc-title");
        tocTitle.TextContent.ShouldBe("Contents");
    }

    [Fact(DisplayName = "Should generate unique container ID by default")]
    public void Render_ShouldGenerateUniqueContainerId_WhenDocumentIdNotProvided()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");

        // Act
        var cut1 = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content));
        var cut2 = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content));

        // Assert
        var container1 = cut1.Find(".neba-document-container");
        var container2 = cut2.Find(".neba-document-container");

        container1.Id.ShouldNotBeNullOrWhiteSpace();
        container2.Id.ShouldNotBeNullOrWhiteSpace();
        container1.Id.ShouldNotBe(container2.Id);
    }

    [Fact(DisplayName = "Should use custom document ID")]
    public void Render_ShouldUseCustomDocumentId_WhenProvided()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.DocumentId, "custom-doc-id"));

        // Assert
        var container = cut.Find(".neba-document-container");
        container.Id.ShouldBe("neba-document-custom-doc-id");
    }

    [Fact(DisplayName = "Should render document container structure with correct IDs")]
    public void Render_ShouldHaveCorrectContainerStructure_WhenDocumentIdProvided()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.DocumentId, "test-doc"));

        // Assert
        cut.Find("#neba-document-test-doc").ShouldNotBeNull();
        cut.Find("#neba-document-toc-test-doc").ShouldNotBeNull();
        cut.Find("#neba-document-toc-test-doc").GetAttribute("aria-label").ShouldBe("Table of Contents");
        cut.Find("#neba-document-content-test-doc").ShouldNotBeNull();
        cut.Find("#neba-document-content-test-doc").ClassList.ShouldContain("neba-panel");
        cut.Find("#neba-document-content-test-doc").ClassList.ShouldContain("neba-document-content");
        cut.Find("#neba-document-toc-list-test-doc").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should invoke OnErrorDismiss callback")]
    public void ErrorDismiss_ShouldInvokeCallback_WhenDismissClicked()
    {
        // Arrange
        var dismissCalled = false;
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.ErrorMessage, "Error message")
            .Add(p => p.OnErrorDismiss, EventCallback.Factory.Create(this, () => dismissCalled = true))
            .Add(p => p.Content, null));

        // Act
        var alert = cut.FindComponent<NebaAlert>();
        var closeButton = alert.Find("button.neba-alert-close");
        closeButton.Click();

        // Assert
        dismissCalled.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should clear error message when dismissed")]
    public void ErrorDismiss_ShouldClearErrorMessage_WhenDismissClicked()
    {
        // Arrange
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.ErrorMessage, "Error message")
            .Add(p => p.Content, null));

        // Act
        var alert = cut.FindComponent<NebaAlert>();
        var closeButton = alert.Find("button.neba-alert-close");
        closeButton.Click();

        // Assert
        cut.FindComponents<NebaAlert>().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should apply neba-panel class to content")]
    public void Render_ShouldApplyPanelClass_ToContentElement()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content));

        // Assert
        var contentDiv = cut.Find(".neba-document-content");
        contentDiv.ClassList.ShouldContain("neba-panel");
    }

    [Fact(DisplayName = "Should use default heading levels of h1, h2")]
    public void Render_ShouldUseDefaultHeadingLevels()
    {
        // Arrange
        var content = new MarkupString("<h1>Heading 1</h1><h2>Heading 2</h2>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content));

        // Assert
        cut.Instance.HeadingLevels.ShouldBe("h1, h2");
    }

    [Fact(DisplayName = "Should accept custom heading levels")]
    public void Render_ShouldAcceptCustomHeadingLevels_WhenProvided()
    {
        // Arrange
        var content = new MarkupString("<h1>H1</h1><h2>H2</h2><h3>H3</h3>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.HeadingLevels, "h1, h2, h3"));

        // Assert
        cut.Instance.HeadingLevels.ShouldBe("h1, h2, h3");
    }

    [Fact(DisplayName = "Should render mobile TOC button")]
    public void Render_ShouldShowMobileTocButton_WhenShowTableOfContentsIsTrue()
    {
        // Arrange
        var content = new MarkupString("<h1>Heading</h1>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.ShowTableOfContents, true));

        // Assert
        var tocButton = cut.Find(".neba-document-toc-mobile-btn");
        tocButton.ShouldNotBeNull();
        tocButton.GetAttribute("aria-label").ShouldBe("Open table of contents");
    }

    [Fact(DisplayName = "Should not render mobile TOC button when ShowTableOfContents is false")]
    public void Render_ShouldHideMobileTocButton_WhenShowTableOfContentsIsFalse()
    {
        // Arrange
        var content = new MarkupString("<h1>Heading</h1>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.ShowTableOfContents, false));

        // Assert
        cut.FindAll(".neba-document-toc-mobile-btn").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render mobile TOC modal")]
    public void Render_ShouldShowMobileTocModal_WhenShowTableOfContentsIsTrue()
    {
        // Arrange
        var content = new MarkupString("<h1>Heading</h1>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.ShowTableOfContents, true)
            .Add(p => p.DocumentId, "test-doc"));

        // Assert
        var tocModal = cut.Find("#neba-document-toc-modal-test-doc");
        tocModal.ShouldNotBeNull();
        tocModal.GetAttribute("role").ShouldBe("dialog");
        tocModal.GetAttribute("aria-modal").ShouldBe("true");
    }

    [Fact(DisplayName = "Should render mobile TOC modal with close button")]
    public void Render_ShouldShowMobileTocModalCloseButton()
    {
        // Arrange
        var content = new MarkupString("<h1>Heading</h1>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.ShowTableOfContents, true)
            .Add(p => p.DocumentId, "test-doc"));

        // Assert
        var closeButton = cut.Find("#neba-document-toc-modal-close-test-doc");
        closeButton.ShouldNotBeNull();
        closeButton.GetAttribute("aria-label").ShouldBe("Close table of contents");
    }

    [Fact(DisplayName = "Should render mobile TOC modal with title matching TableOfContentsTitle")]
    public void Render_ShouldMatchMobileTocModalTitle_ToTableOfContentsTitle()
    {
        // Arrange
        var content = new MarkupString("<h1>Heading</h1>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.TableOfContentsTitle, "Custom Mobile Title")
            .Add(p => p.DocumentId, "test-doc"));

        // Assert
        var modalTitle = cut.Find("#neba-document-toc-modal-title-test-doc");
        modalTitle.TextContent.ShouldBe("Custom Mobile Title");
    }

    [Fact(DisplayName = "Should render mobile TOC button with icon and text")]
    public void Render_ShouldShowMobileTocButtonWithIconAndText()
    {
        // Arrange
        var content = new MarkupString("<h1>Heading</h1>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.ShowTableOfContents, true));

        // Assert
        var tocButton = cut.Find(".neba-document-toc-mobile-btn");
        var svg = tocButton.QuerySelector("svg");
        svg.ShouldNotBeNull();
        svg!.GetAttribute("aria-hidden").ShouldBe("true");

        var span = tocButton.QuerySelector("span");
        span.ShouldNotBeNull();
        span!.TextContent.ShouldBe("Contents");
    }

    [Fact(DisplayName = "Should render TOC sticky container")]
    public void Render_ShouldShowTocStickyContainer()
    {
        // Arrange
        var content = new MarkupString("<h1>Heading</h1>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.ShowTableOfContents, true));

        // Assert
        var stickyContainer = cut.Find(".toc-sticky");
        stickyContainer.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should render slideover panel")]
    public void Render_ShouldShowSlideoverPanel()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.DocumentId, "test-doc"));

        // Assert
        var slideover = cut.Find("#neba-document-slideover-test-doc");
        slideover.ShouldNotBeNull();
        slideover.GetAttribute("role").ShouldBe("dialog");
        slideover.GetAttribute("aria-modal").ShouldBe("true");
    }

    [Fact(DisplayName = "Should render slideover panel with close button")]
    public void Render_ShouldShowSlideoverCloseButton()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.DocumentId, "test-doc"));

        // Assert
        var closeButton = cut.Find("#neba-document-slideover-close-test-doc");
        closeButton.ShouldNotBeNull();
        closeButton.GetAttribute("aria-label").ShouldBe("Close document");
    }

    [Fact(DisplayName = "Should render slideover content area")]
    public void Render_ShouldShowSlideoverContentArea()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.DocumentId, "test-doc"));

        // Assert
        var slideoverContent = cut.Find("#neba-document-slideover-content-test-doc");
        slideoverContent.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should render slideover with default loading title")]
    public void Render_ShouldShowSlideoverDefaultTitle_WhenSlideoverTitleIsNull()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.DocumentId, "test-doc"));

        // Assert
        var slideoverTitle = cut.Find("#neba-document-slideover-title-test-doc");
        slideoverTitle.TextContent.ShouldBe("Loading...");
    }

    [Fact(DisplayName = "Should render slideover with custom title")]
    public void Render_ShouldShowSlideoverCustomTitle_WhenSlideoverTitleIsSet()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.DocumentId, "test-doc")
            .Add(p => p.SlideoverTitle, "NEBA Bylaws"));

        // Assert
        var slideoverTitle = cut.Find("#neba-document-slideover-title-test-doc");
        slideoverTitle.TextContent.ShouldBe("NEBA Bylaws");
    }

    [Fact(DisplayName = "Should render slideover loading state")]
    public void Render_ShouldShowSlideoverLoadingState_WhenSlideoverIsLoadingIsTrue()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.SlideoverIsLoading, true));

        // Assert
        var slideoverContent = cut.Find(".neba-document-slideover-content");
        slideoverContent.InnerHtml.ShouldContain("Loading document...");
    }

    [Fact(DisplayName = "Should render slideover content when provided")]
    public void Render_ShouldShowSlideoverContent_WhenSlideoverContentIsSet()
    {
        // Arrange
        var content = new MarkupString("<p>Main Content</p>");
        var slideoverContent = new MarkupString("<p>Slideover HTML</p>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.SlideoverContent, slideoverContent)
            .Add(p => p.SlideoverIsLoading, false));

        // Assert
        var slideoverContentElement = cut.Find(".neba-document-slideover-content");
        slideoverContentElement.InnerHtml.ShouldContain("Slideover HTML");
    }

    [Fact(DisplayName = "Should not render last updated in TOC when CachedAt is null")]
    public void Render_ShouldNotShowLastUpdated_WhenCachedAtIsNull()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.CachedAt, (DateTimeOffset?)null));

        // Assert
        cut.FindAll(".neba-document-toc-last-updated").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render last updated in TOC when CachedAt is provided")]
    public void Render_ShouldShowLastUpdatedInToc_WhenCachedAtIsProvided()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");
        var cachedAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.CachedAt, cachedAt));

        // Assert
        var tocLastUpdated = cut.Find(".neba-document-toc-last-updated");
        tocLastUpdated.ShouldNotBeNull();
        tocLastUpdated.TextContent.ShouldContain("Last updated:");
        tocLastUpdated.TextContent.ShouldContain("January 15, 2024");
    }

    [Fact(DisplayName = "Should generate unique IDs for all TOC elements")]
    public void Render_ShouldGenerateUniqueIds_ForAllTocElements()
    {
        // Arrange
        var content = new MarkupString("<h1>Heading</h1>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.DocumentId, "test-doc"));

        // Assert
        cut.Find("#neba-document-toc-test-doc").ShouldNotBeNull();
        cut.Find("#neba-document-toc-list-test-doc").ShouldNotBeNull();
        cut.Find("#neba-document-toc-mobile-btn-test-doc").ShouldNotBeNull();
        cut.Find("#neba-document-toc-modal-test-doc").ShouldNotBeNull();
        cut.Find("#neba-document-toc-modal-overlay-test-doc").ShouldNotBeNull();
        cut.Find("#neba-document-toc-modal-close-test-doc").ShouldNotBeNull();
        cut.Find("#neba-document-toc-modal-title-test-doc").ShouldNotBeNull();
        cut.Find("#neba-document-toc-mobile-list-test-doc").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should generate unique IDs for slideover elements")]
    public void Render_ShouldGenerateUniqueIds_ForSlideoverElements()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.DocumentId, "test-doc"));

        // Assert
        cut.Find("#neba-document-slideover-test-doc").ShouldNotBeNull();
        cut.Find("#neba-document-slideover-overlay-test-doc").ShouldNotBeNull();
        cut.Find("#neba-document-slideover-close-test-doc").ShouldNotBeNull();
        cut.Find("#neba-document-slideover-title-test-doc").ShouldNotBeNull();
        cut.Find("#neba-document-slideover-content-test-doc").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should render multiple instances with different IDs")]
    public void Render_ShouldHaveDifferentIds_WhenMultipleInstancesRendered()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");

        // Act
        var cut1 = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.DocumentId, "doc-1"));

        var cut2 = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.DocumentId, "doc-2"));

        // Assert
        cut1.Find("#neba-document-doc-1").ShouldNotBeNull();
        cut1.Find("#neba-document-content-doc-1").ShouldNotBeNull();
        cut1.Find("#neba-document-toc-doc-1").ShouldNotBeNull();

        cut2.Find("#neba-document-doc-2").ShouldNotBeNull();
        cut2.Find("#neba-document-content-doc-2").ShouldNotBeNull();
        cut2.Find("#neba-document-toc-doc-2").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should have accessible TOC navigation")]
    public void Render_ShouldHaveAccessibleTocNavigation()
    {
        // Arrange
        var content = new MarkupString("<h1>Heading</h1>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.ShowTableOfContents, true));

        // Assert
        var nav = cut.Find("nav.neba-document-toc");
        nav.GetAttribute("aria-label").ShouldBe("Table of Contents");
    }

    [Fact(DisplayName = "Should render close buttons with accessible labels")]
    public void Render_ShouldHaveAccessibleCloseButtons()
    {
        // Arrange
        var content = new MarkupString("<h1>Heading</h1>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.ShowTableOfContents, true)
            .Add(p => p.DocumentId, "test-doc"));

        // Assert
        var tocCloseButton = cut.Find("#neba-document-toc-modal-close-test-doc");
        tocCloseButton.GetAttribute("aria-label").ShouldBe("Close table of contents");

        var slideoverCloseButton = cut.Find("#neba-document-slideover-close-test-doc");
        slideoverCloseButton.GetAttribute("aria-label").ShouldBe("Close document");
    }

    [Fact(DisplayName = "Should render icons with aria-hidden true")]
    public void Render_ShouldHideIconsFromScreenReaders()
    {
        // Arrange
        var content = new MarkupString("<h1>Heading</h1>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.ShowTableOfContents, true));

        // Assert
        var svgElements = cut.FindAll("svg");
        svgElements.ShouldNotBeEmpty();

        foreach (var svg in svgElements)
        {
            svg.GetAttribute("aria-hidden").ShouldBe("true");
        }
    }

    [Fact(DisplayName = "Should not render TOC or slideover when content is null")]
    public void Render_ShouldNotShowTocOrSlideover_WhenContentIsNull()
    {
        // Arrange & Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, null)
            .Add(p => p.IsLoading, false));

        // Assert
        cut.FindAll(".neba-document-container").ShouldBeEmpty();
        cut.FindAll(".neba-document-toc").ShouldBeEmpty();
        cut.FindAll(".neba-document-slideover").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should not render TOC or slideover when IsLoading is true")]
    public void Render_ShouldNotShowTocOrSlideover_WhenIsLoadingIsTrue()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.IsLoading, true));

        // Assert
        cut.FindAll(".neba-document-container").ShouldBeEmpty();
        cut.FindAll(".neba-document-toc").ShouldBeEmpty();
        cut.FindAll(".neba-document-slideover").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should include module script tag")]
    public void Render_ShouldIncludeModuleScriptTag()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content));

        // Assert
        var scriptTag = cut.Find("script[src='./Documents/NebaDocument.razor.js']");
        scriptTag.ShouldNotBeNull();
        scriptTag.GetAttribute("type").ShouldBe("module");
    }

    [Fact(DisplayName = "Should include stylesheet link")]
    public void Render_ShouldIncludeStylesheetLink()
    {
        // Arrange
        var content = new MarkupString("<p>Content</p>");

        // Act
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content));

        // Assert
        var linkTag = cut.Find("link[href='/neba-document.css']");
        linkTag.ShouldNotBeNull();
        linkTag.GetAttribute("rel").ShouldBe("stylesheet");
    }

    [Fact(DisplayName = "Should dispose without throwing exception")]
    public async Task DisposeAsync_ShouldNotThrow()
    {
        // Arrange
        var content = new MarkupString("<h1>Test</h1>");
        var cut = _ctx.Render<NebaDocument>(parameters => parameters
            .Add(p => p.Content, content)
            .Add(p => p.ShowTableOfContents, true));

        // Act & Assert
        IAsyncDisposable disposable = cut.Instance;
        await disposable.DisposeAsync();

        cut.Instance.ShouldNotBeNull();
    }
}