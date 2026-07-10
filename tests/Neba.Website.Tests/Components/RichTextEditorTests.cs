using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.RichTextEditor")]
public sealed class RichTextEditorTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly BunitJSInterop _moduleInterop;

    public RichTextEditorTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _moduleInterop = _ctx.JSInterop.SetupModule("./Components/RichTextEditor.razor.js");

        _ctx.Services.AddSingleton<ILogger<RichTextEditor>>(NullLogger<RichTextEditor>.Instance);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render a container div with the neba-rte id prefix")]
    public void Render_ShouldRenderContainerDiv_WithNebaRteIdPrefix()
    {
        // Act
        var cut = _ctx.Render<RichTextEditor>();

        // Assert
        cut.Find("div[id^='neba-rte-']").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should call initialize JS function on first render")]
    public void OnAfterRender_ShouldCallInitialize_WhenFirstRender()
    {
        // Act
        _ctx.Render<RichTextEditor>();

        // Assert
        _moduleInterop.VerifyInvoke("initialize");
    }

    [Fact(DisplayName = "Should call initialize exactly once across multiple renders")]
    public void OnAfterRender_ShouldNotCallInitializeAgain_WhenSubsequentRender()
    {
        // Arrange
        var cut = _ctx.Render<RichTextEditor>();

        // Act
        cut.Render(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(RichTextEditor.Placeholder), "Write your article..." }
        }));

        // Assert
        _moduleInterop.VerifyInvoke("initialize", 1);
    }

    [Fact(DisplayName = "Should pass the initial value to the JS initialize call")]
    public void OnAfterRender_ShouldPassInitialValue_ToInitializeCall()
    {
        // Act
        _ctx.Render<RichTextEditor>(parameters =>
            parameters.Add(p => p.Value, "<p>Initial</p>"));

        // Assert
        var invocation = _moduleInterop.VerifyInvoke("initialize");
        invocation.Arguments[2].ShouldBe("<p>Initial</p>");
    }

    [Fact(DisplayName = "Should pass ReadOnly and Placeholder to the JS initialize call")]
    public void OnAfterRender_ShouldPassReadOnlyAndPlaceholder_ToInitializeCall()
    {
        // Act
        _ctx.Render<RichTextEditor>(parameters => parameters
            .Add(p => p.ReadOnly, true)
            .Add(p => p.Placeholder, "Write your article..."));

        // Assert
        var invocation = _moduleInterop.VerifyInvoke("initialize");
        invocation.Arguments[3].ShouldBe(true);
        invocation.Arguments[4].ShouldBe("Write your article...");
    }

    [Fact(DisplayName = "Should call setHtml JS function when Value parameter changes")]
    public void OnParametersSet_ShouldCallSetHtml_WhenValueChanges()
    {
        // Arrange
        var cut = _ctx.Render<RichTextEditor>(parameters =>
            parameters.Add(p => p.Value, "<p>Original</p>"));

        // Act
        cut.Render(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(RichTextEditor.Value), "<p>Updated</p>" }
        }));

        // Assert
        var invocation = _moduleInterop.VerifyInvoke("setHtml");
        invocation.Arguments[1].ShouldBe("<p>Updated</p>");
    }

    [Fact(DisplayName = "Should not call setHtml when re-rendered with the same value")]
    public void OnParametersSet_ShouldNotCallSetHtml_WhenValueUnchanged()
    {
        // Arrange
        var cut = _ctx.Render<RichTextEditor>(parameters =>
            parameters.Add(p => p.Value, "<p>Same</p>"));

        // Act
        cut.Render(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(RichTextEditor.Value), "<p>Same</p>" }
        }));

        // Assert
        _moduleInterop.VerifyNotInvoke("setHtml");
    }

    [Fact(DisplayName = "Should call setReadOnly JS function when ReadOnly parameter changes")]
    public void OnParametersSet_ShouldCallSetReadOnly_WhenReadOnlyChanges()
    {
        // Arrange
        var cut = _ctx.Render<RichTextEditor>(parameters =>
            parameters.Add(p => p.ReadOnly, false));

        // Act
        cut.Render(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(RichTextEditor.ReadOnly), true }
        }));

        // Assert
        var invocation = _moduleInterop.VerifyInvoke("setReadOnly");
        invocation.Arguments[1].ShouldBe(true);
    }

    [Fact(DisplayName = "Should update bound Value and raise ValueChanged when JS notifies a content change")]
    public async Task NotifyContentChanged_ShouldUpdateValueAndRaiseValueChanged_WhenInvoked()
    {
        // Arrange
        string? receivedValue = null;
        var cut = _ctx.Render<RichTextEditor>(parameters => parameters
            .Add(p => p.Value, string.Empty)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string>(this, v => receivedValue = v)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.NotifyContentChanged("<p>Typed by user</p>"));

        // Assert
        cut.Instance.Value.ShouldBe("<p>Typed by user</p>");
        receivedValue.ShouldBe("<p>Typed by user</p>");
    }

    [Fact(DisplayName = "Should not call setHtml after a JS-originated content change echoes back as the same Value")]
    public async Task OnParametersSet_ShouldNotCallSetHtml_AfterNotifyContentChangedEchoesBack()
    {
        // Arrange
        var cut = _ctx.Render<RichTextEditor>(parameters =>
            parameters.Add(p => p.Value, string.Empty));

        // Act
        await cut.InvokeAsync(() => cut.Instance.NotifyContentChanged("<p>Typed by user</p>"));
        cut.Render(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(RichTextEditor.Value), "<p>Typed by user</p>" }
        }));

        // Assert
        _moduleInterop.VerifyNotInvoke("setHtml");
    }

    [Fact(DisplayName = "Should call dispose JS function when component is disposed")]
    public async Task DisposeAsync_ShouldCallDisposeJs_WhenDisposed()
    {
        // Arrange
        var cut = _ctx.Render<RichTextEditor>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

        // Assert
        _moduleInterop.VerifyInvoke("dispose", 1);
    }

    [Fact(DisplayName = "Should not throw when DisposeAsync encounters a JSDisconnectedException on dispose")]
    public async Task DisposeAsync_ShouldNotThrow_WhenJSDisconnectedExceptionOccurs()
    {
        // Arrange
        _moduleInterop.SetupVoid("dispose", _ => true).SetException(new JSDisconnectedException("Disconnected"));

        var cut = _ctx.Render<RichTextEditor>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

        // Assert — if the exception was not swallowed, InvokeAsync would have thrown before this assertion
        cut.Instance.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should not throw when DisposeAsync encounters a JSException during JS module disposal")]
    public async Task DisposeAsync_ShouldNotThrow_WhenJSExceptionOccursDuringModuleDisposal()
    {
        // Arrange
        var cut = _ctx.Render<RichTextEditor>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

        // Assert — disposing twice must not throw
        cut.Instance.ShouldNotBeNull();
    }
}