using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Documents;

namespace Neba.Website.Tests.Documents;

[UnitTest]
[Component("Website.Documents.DocumentSectionSlideover")]
public sealed class DocumentSectionSlideoverTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public DocumentSectionSlideoverTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<DocumentSectionSlideover> RenderSlideover(
        bool isOpen = true,
        Action? onClose = null,
        string? content = "<p>Eligibility</p>",
        bool isLoading = false,
        string? error = null,
        DateTimeOffset? lastUpdated = null)
        => _ctx.Render<DocumentSectionSlideover>(p => p
            .Add(x => x.IsOpen, isOpen)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => onClose?.Invoke()))
            .Add(x => x.Title, "Invitational rules")
            .Add(x => x.FullDocumentHref, "/tournaments/rules#invitational")
            .Add(x => x.Content, content is null ? null : new MarkupString(content))
            .Add(x => x.IsLoading, isLoading)
            .Add(x => x.LoadingText, "Loading...")
            .Add(x => x.CloseLabel, "Close")
            .Add(x => x.ErrorMessage, error)
            .Add(x => x.LastUpdated, lastUpdated));

    [Fact(DisplayName = "Closed slide-out renders no dialog")]
    public void Render_ShouldRenderNothing_WhenClosed()
    {
        // Arrange & Act
        var cut = RenderSlideover(isOpen: false);

        // Assert
        cut.FindAll("[role=dialog]").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Open slide-out renders title, content and the full-page link")]
    public void Render_ShouldRenderTitleContentAndFooterLink_WhenOpen()
    {
        // Arrange & Act
        var cut = RenderSlideover();

        // Assert
        cut.Find(".neba-document-slideover-title").TextContent.ShouldBe("Invitational rules");
        cut.Find(".neba-document-slideover-content").TextContent.ShouldContain("Eligibility");
        cut.Find(".neba-document-slideover-footer a").GetAttribute("href").ShouldBe("/tournaments/rules#invitational");
    }

    [Fact(DisplayName = "Close button calls OnClose")]
    public void CloseButton_ShouldInvokeOnClose()
    {
        // Arrange
        var closed = 0;
        var cut = RenderSlideover(onClose: () => closed++);

        // Act
        cut.Find("button.neba-document-slideover-close").Click();

        // Assert
        closed.ShouldBe(1);
    }

    [Fact(DisplayName = "Overlay click calls OnClose")]
    public void Overlay_ShouldInvokeOnClose_WhenClicked()
    {
        // Arrange
        var closed = 0;
        var cut = RenderSlideover(onClose: () => closed++);

        // Act
        cut.Find(".neba-document-slideover-overlay").Click();

        // Assert
        closed.ShouldBe(1);
    }

    [Theory(DisplayName = "Escape closes the slide-out and other keys do not")]
    [InlineData("Escape", 1)]
    [InlineData("Esc", 1)]
    [InlineData("a", 0)]
    public void KeyDown_ShouldInvokeOnClose_OnlyForEscape(string key, int expected)
    {
        // Arrange
        var closed = 0;
        var cut = RenderSlideover(onClose: () => closed++);

        // Act
        cut.Find("[role=dialog]").KeyDown(new KeyboardEventArgs { Key = key });

        // Assert
        closed.ShouldBe(expected);
    }

    [Fact(DisplayName = "Loading state shows loading text instead of content")]
    public void Render_ShouldShowLoadingText_WhenLoading()
    {
        // Arrange & Act
        var cut = RenderSlideover(isLoading: true);

        // Assert
        cut.Find(".neba-document-slideover-content").TextContent.ShouldContain("Loading...");
        cut.Find(".neba-document-slideover-content").TextContent.ShouldNotContain("Eligibility");
    }

    [Fact(DisplayName = "Error state shows the error message")]
    public void Render_ShouldShowError_WhenErrorMessageSet()
    {
        // Arrange & Act
        var cut = RenderSlideover(content: null, error: "Could not load.");

        // Assert
        cut.Find(".neba-document-slideover-status").TextContent.ShouldBe("Could not load.");
    }

    [Fact(DisplayName = "Last-updated text shows only when set")]
    public void Render_ShouldShowLastUpdated_OnlyWhenSet()
    {
        // Arrange & Act
        var without = RenderSlideover();
        var with = RenderSlideover(lastUpdated: new DateTimeOffset(2026, 3, 5, 0, 0, 0, TimeSpan.Zero));

        // Assert
        without.FindAll(".neba-document-slideover-updated").ShouldBeEmpty();
        with.Find(".neba-document-slideover-updated").TextContent.ShouldBe("Last updated: March 5, 2026");
    }
}