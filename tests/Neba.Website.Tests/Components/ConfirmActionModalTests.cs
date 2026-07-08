using Bunit;

using Microsoft.AspNetCore.Components;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.ConfirmActionModal")]
public sealed class ConfirmActionModalTests : IDisposable
{
    private readonly BunitContext _ctx;

    public ConfirmActionModalTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render nothing when IsOpen is false")]
    public void Render_ShouldRenderNothing_WhenIsOpenIsFalse()
    {
        // Arrange & Act
        var cut = RenderModal(isOpen: false);

        // Assert
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render title and message when IsOpen is true")]
    public void Render_ShouldShowTitleAndMessage_WhenIsOpenIsTrue()
    {
        // Arrange & Act
        var cut = RenderModal(isOpen: true, title: "Delete article?", message: "This can't be undone.");

        // Assert
        cut.Markup.ShouldContain("Delete article?");
        cut.Markup.ShouldContain("This can't be undone.");
    }

    [Fact(DisplayName = "Should render default Cancel and Delete labels")]
    public void Render_ShouldShowDefaultLabels_WhenLabelsNotSpecified()
    {
        // Arrange & Act
        var cut = RenderModal(isOpen: true);

        // Assert
        cut.Find("button.confirm-action-modal-cancel").TextContent.ShouldBe("Cancel");
        cut.Find("button.confirm-action-modal-confirm").TextContent.Trim().ShouldBe("Delete");
    }

    [Fact(DisplayName = "Should invoke OnConfirm when the confirm button is clicked")]
    public void Click_ShouldInvokeOnConfirm_WhenConfirmButtonIsClicked()
    {
        // Arrange
        var confirmed = false;
        var cut = RenderModal(isOpen: true, onConfirm: () => confirmed = true);

        // Act
        cut.Find("button.confirm-action-modal-confirm").Click();

        // Assert
        confirmed.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should invoke OnCancel when the cancel button is clicked")]
    public void Click_ShouldInvokeOnCancel_WhenCancelButtonIsClicked()
    {
        // Arrange
        var cancelled = false;
        var cut = RenderModal(isOpen: true, onCancel: () => cancelled = true);

        // Act
        cut.Find("button.confirm-action-modal-cancel").Click();

        // Assert
        cancelled.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should disable both buttons and show busy label when IsBusy is true")]
    public void Render_ShouldDisableButtonsAndShowBusyLabel_WhenIsBusyIsTrue()
    {
        // Arrange & Act
        var cut = RenderModal(isOpen: true, isBusy: true);

        // Assert
        cut.Find("button.confirm-action-modal-cancel").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("button.confirm-action-modal-confirm").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("button.confirm-action-modal-confirm").TextContent.Trim().ShouldBe("Deleting...");
    }

    private IRenderedComponent<ConfirmActionModal> RenderModal(
        bool isOpen,
        string title = "Delete item?",
        string message = "This can't be undone.",
        bool isBusy = false,
        Action? onConfirm = null,
        Action? onCancel = null)
        => _ctx.Render<ConfirmActionModal>(p => p
            .Add(x => x.IsOpen, isOpen)
            .Add(x => x.Title, title)
            .Add(x => x.Message, message)
            .Add(x => x.IsBusy, isBusy)
            .Add(x => x.OnConfirm, EventCallback.Factory.Create(this, onConfirm ?? (() => { })))
            .Add(x => x.OnCancel, EventCallback.Factory.Create(this, onCancel ?? (() => { }))));
}
