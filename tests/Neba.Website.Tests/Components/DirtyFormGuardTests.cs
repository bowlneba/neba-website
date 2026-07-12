using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.DirtyFormGuard")]
public sealed class DirtyFormGuardTests : IDisposable
{
    private readonly BunitContext _ctx;

    public DirtyFormGuardTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should not show a confirmation prompt when navigating away and the form is not dirty")]
    public void Navigate_ShouldNotShowPrompt_WhenFormIsNotDirty()
    {
        // Arrange
        var cut = _ctx.Render<DirtyFormGuard>(p => p.Add(x => x.IsDirty, false));
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();

        // Act
        nav.NavigateTo("/news");

        // Assert
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
        nav.Uri.ShouldEndWith("/news");
    }

    [Fact(DisplayName = "Should show a confirmation prompt and block navigation when the form is dirty")]
    public void Navigate_ShouldShowPromptAndBlockNavigation_WhenFormIsDirty()
    {
        // Arrange
        var cut = _ctx.Render<DirtyFormGuard>(p => p.Add(x => x.IsDirty, true));
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var originalUri = nav.Uri;

        // Act
        nav.NavigateTo("/news");

        // Assert
        cut.Find(".neba-modal-backdrop").ShouldNotBeNull();
        nav.Uri.ShouldBe(originalUri);
    }

    [Fact(DisplayName = "Should complete navigation when the user confirms leaving")]
    public void Confirm_ShouldCompleteNavigation_WhenUserConfirmsLeaving()
    {
        // Arrange
        var cut = _ctx.Render<DirtyFormGuard>(p => p.Add(x => x.IsDirty, true));
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/news");

        // Act
        cut.Find("button.confirm-action-modal-confirm").Click();

        // Assert
        nav.Uri.ShouldEndWith("/news");
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should stay on the page and close the prompt when the user cancels leaving")]
    public void Cancel_ShouldStayOnPageAndClosePrompt_WhenUserCancelsLeaving()
    {
        // Arrange
        var cut = _ctx.Render<DirtyFormGuard>(p => p.Add(x => x.IsDirty, true));
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var originalUri = nav.Uri;
        nav.NavigateTo("/news");

        // Act
        cut.Find("button.confirm-action-modal-cancel").Click();

        // Assert
        nav.Uri.ShouldBe(originalUri);
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render default title, message, and button labels")]
    public void Render_ShouldShowDefaultLabels_WhenNotSpecified()
    {
        // Arrange
        var cut = _ctx.Render<DirtyFormGuard>(p => p.Add(x => x.IsDirty, true));
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();

        // Act
        nav.NavigateTo("/news");

        // Assert
        cut.Markup.ShouldContain("Discard unsaved changes?");
        cut.Markup.ShouldContain("You have unsaved changes. If you leave this page now, they will be lost.");
        cut.Find("button.confirm-action-modal-confirm").TextContent.Trim().ShouldBe("Leave");
        cut.Find("button.confirm-action-modal-cancel").TextContent.ShouldBe("Stay");
    }
}