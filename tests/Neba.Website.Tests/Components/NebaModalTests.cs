using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.NebaModal")]
public sealed class NebaModalTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly BunitJSInterop _modalModuleInterop;

    public NebaModalTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _modalModuleInterop = _ctx.JSInterop.SetupModule("./Components/NebaModal.razor.js");
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render nothing when IsOpen is false")]
    public void Render_ShouldRenderNothing_WhenIsOpenIsFalse()
    {
        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, false)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { })));

        cut.FindAll(".neba-modal-backdrop").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should render backdrop when IsOpen is true")]
    public void Render_ShouldRenderBackdrop_WhenIsOpenIsTrue()
    {
        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { })));

        cut.Find(".neba-modal-backdrop").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should enable focus trap when modal opens")]
    public void OnAfterRender_ShouldEnableFocusTrap_WhenModalOpens()
    {
        _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { })));

        _modalModuleInterop.VerifyInvoke("focusElement", 1);
        _modalModuleInterop.VerifyInvoke("enableFocusTrap", 1);
    }

    [Fact(DisplayName = "Should apply dialog ARIA semantics when modal is open")]
    public void Render_ShouldApplyDialogAriaSemantics_WhenModalIsOpen()
    {
        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.Title, "Accessible title"));

        var dialog = cut.Find(".neba-modal-content");

        dialog.GetAttribute("role").ShouldBe("dialog");
        dialog.GetAttribute("aria-modal").ShouldBe("true");
        dialog.GetAttribute("aria-label").ShouldBeNull();

        var labelledBy = dialog.GetAttribute("aria-labelledby") ?? string.Empty;
        labelledBy.ShouldNotBeEmpty();
        var titleClassName = cut.Find($"#{labelledBy}").ClassName ?? string.Empty;
        titleClassName.ShouldContain("neba-modal-title");

        var describedBy = dialog.GetAttribute("aria-describedby") ?? string.Empty;
        describedBy.ShouldNotBeEmpty();
        var bodyClassName = cut.Find($"#{describedBy}").ClassName ?? string.Empty;
        bodyClassName.ShouldContain("neba-modal-body");
    }

    [Fact(DisplayName = "Should omit aria-labelledby when title is not provided")]
    public void Render_ShouldOmitAriaLabelledBy_WhenTitleIsNotProvided()
    {
        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { })));

        var dialog = cut.Find(".neba-modal-content");

        dialog.GetAttribute("aria-labelledby").ShouldBeNull();
        dialog.GetAttribute("aria-label").ShouldBe("Dialog");
    }

    [Fact(DisplayName = "Should render title when Title is provided")]
    public void Render_ShouldRenderTitle_WhenTitleIsProvided()
    {
        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.Title, "Test Title"));

        cut.Find(".neba-modal-header").ShouldNotBeNull();
        cut.Markup.ShouldContain("Test Title");
    }

    [Fact(DisplayName = "Should not render header when Title is null")]
    public void Render_ShouldNotRenderHeader_WhenTitleIsNull()
    {
        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { })));

        cut.FindAll(".neba-modal-header").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should render close button when ShowCloseButton is true")]
    public void Render_ShouldRenderCloseButton_WhenShowCloseButtonIsTrue()
    {
        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.Title, "Modal Title")
            .Add(x => x.ShowCloseButton, true));

        cut.Find(".neba-modal-close").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should not render close button when ShowCloseButton is false")]
    public void Render_ShouldNotRenderCloseButton_WhenShowCloseButtonIsFalse()
    {
        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.Title, "Modal Title")
            .Add(x => x.ShowCloseButton, false));

        cut.FindAll(".neba-modal-close").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should render child content when provided")]
    public void Render_ShouldRenderChildContent_WhenProvided()
    {
        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.ChildContent, builder => builder.AddMarkupContent(0, "<p>Modal body content</p>")));

        cut.Find(".neba-modal-body").ShouldNotBeNull();
        cut.Markup.ShouldContain("Modal body content");
    }

    [Fact(DisplayName = "Should render footer when FooterContent is provided")]
    public void Render_ShouldRenderFooter_WhenFooterContentProvided()
    {
        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.FooterContent, builder => builder.AddMarkupContent(0, "<button>OK</button>")));

        cut.Find(".neba-modal-footer").ShouldNotBeNull();
        cut.Markup.ShouldContain("OK");
    }

    [Fact(DisplayName = "Should not render footer when FooterContent is null")]
    public void Render_ShouldNotRenderFooter_WhenFooterContentIsNull()
    {
        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { })));

        cut.FindAll(".neba-modal-footer").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should invoke OnClose when close button is clicked")]
    public async Task HandleClose_ShouldInvokeOnClose_WhenCloseButtonClicked()
    {
        var closeCalled = false;

        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closeCalled = true))
            .Add(x => x.Title, "Modal Title")
            .Add(x => x.ShowCloseButton, true));

        await cut.Find(".neba-modal-close").ClickAsync(new());

        closeCalled.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should invoke OnClose when backdrop is clicked and CloseOnBackdropClick is true")]
    public async Task HandleBackdropClick_ShouldInvokeOnClose_WhenCloseOnBackdropClickIsTrue()
    {
        var closeCalled = false;

        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closeCalled = true))
            .Add(x => x.CloseOnBackdropClick, true));

        await cut.Find(".neba-modal-backdrop").ClickAsync(new());

        closeCalled.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should not invoke OnClose when backdrop is clicked and CloseOnBackdropClick is false")]
    public async Task HandleBackdropClick_ShouldNotInvokeOnClose_WhenCloseOnBackdropClickIsFalse()
    {
        var closeCalled = false;

        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closeCalled = true))
            .Add(x => x.CloseOnBackdropClick, false));

        await cut.Find(".neba-modal-backdrop").ClickAsync(new());

        closeCalled.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should invoke OnClose when Escape is pressed and escape close is enabled")]
    public async Task HandleKeyDown_ShouldInvokeOnClose_WhenEscapePressedAndEscapeCloseIsEnabled()
    {
        var closeCalled = false;

        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closeCalled = true))
            .Add(x => x.ShowCloseButton, false)
            .Add(x => x.CloseOnBackdropClick, true));

        await cut.Find(".neba-modal-content")
            .TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "Escape" });

        closeCalled.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should not invoke OnClose when Escape is pressed and escape close is disabled")]
    public async Task HandleKeyDown_ShouldNotInvokeOnClose_WhenEscapePressedAndEscapeCloseIsDisabled()
    {
        var closeCalled = false;

        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closeCalled = true))
            .Add(x => x.ShowCloseButton, false)
            .Add(x => x.CloseOnBackdropClick, false));

        await cut.Find(".neba-modal-content")
            .TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "Escape" });

        closeCalled.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should apply CssClass to modal content when provided")]
    public void Render_ShouldApplyCssClass_WhenProvided()
    {
        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.CssClass, "my-custom-class"));

        cut.Find(".neba-modal-content.my-custom-class").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should apply MaxWidth to modal container when provided")]
    public void Render_ShouldApplyMaxWidthStyle_WhenProvided()
    {
        var cut = _ctx.Render<NebaModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.MaxWidth, "700px"));

        var containerStyle = cut.Find(".neba-modal-container").GetAttribute("style") ?? string.Empty;

        containerStyle.ShouldContain("max-width: 700px;");
    }
}
