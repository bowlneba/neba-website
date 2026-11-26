using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

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

    private const string ModulePath = "./Components/NebaModal.razor.js";

    [Fact(DisplayName = "Closing returns focus even while the open calls are still running")]
    public void Close_ShouldReturnFocus_WhenOpenCallsAreStillRunning()
    {
        // Arrange: enableFocusTrap never completes, so the open render never finishes.
        var module = _ctx.JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        module.SetupVoid("enableFocusTrap", _ => true);
        var cut = RenderSlideover(isOpen: false);
        cut.Render(p => p.Add(x => x.ReturnFocusTo, new ElementReference("trigger")));
        cut.Render(p => p.Add(x => x.IsOpen, true));

        // Act
        cut.Render(p => p.Add(x => x.IsOpen, false));

        // Assert
        module.Invocations["focusElement"][^1].Arguments[0]
            .ShouldBeOfType<ElementReference>().Id.ShouldBe("trigger");
    }

    [Fact(DisplayName = "Opening before the script has loaded still moves focus into the dialog")]
    public async Task Open_ShouldFocusDialog_WhenScriptLoadsLate()
    {
        // Arrange
        await using var js = new DeferredImportJsRuntime();
        _ctx.Services.AddSingleton<IJSRuntime>(js);
        var cut = RenderSlideover(isOpen: false);
        cut.Render(p => p.Add(x => x.IsOpen, true));

        // Act
        await cut.InvokeAsync(js.CompleteImport);

        // Assert
        await cut.WaitForAssertionAsync(() => js.Calls.ShouldContain("focusElement"));
    }

    // Holds the module import open until the test releases it.
    private sealed class DeferredImportJsRuntime : IJSRuntime, IJSObjectReference
    {
        private readonly TaskCompletionSource<IJSObjectReference> _import = new();

        public List<string> Calls { get; } = [];

        public void CompleteImport() => _import.SetResult(this);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "import")
            {
                return new ValueTask<TValue>(_import.Task.ContinueWith(
                    task => (TValue)task.Result, TaskScheduler.Default));
            }

            Calls.Add(identifier);
            return ValueTask.FromResult<TValue>(default!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

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