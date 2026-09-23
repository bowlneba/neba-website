using Bunit;

using Microsoft.AspNetCore.Components;

using Neba.Api.Contracts.Security;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.Documents;

namespace Neba.Website.Tests.Documents;

[UnitTest]
[Component("Website.Documents.DocumentLastUpdatedRow")]
public sealed class DocumentLastUpdatedRowTests : IDisposable
{
    private const string RefreshButtonSelector = "button[title='Refresh from Google Drive']";

    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    private void AuthorizeWithRefreshPermission()
        => _ctx.AddAuthorization().SetAuthorized("test-user").SetPolicies(Permissions.RefreshDocument.PolicyName);

    [Fact(DisplayName = "Should render nothing when there is no last-updated text and refresh is unavailable")]
    public void Render_ShouldRenderNothing_WhenNoTextAndCannotRefresh()
    {
        // Arrange & Act
        var cut = _ctx.Render<DocumentLastUpdatedRow>(parameters => parameters
            .Add(p => p.LastUpdatedText, null)
            .Add(p => p.CanRefresh, false));

        // Assert
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Theory(DisplayName = "Should treat empty or whitespace last-updated text as absent")]
    [InlineData("", TestDisplayName = "Empty")]
    [InlineData("   ", TestDisplayName = "Whitespace")]
    public void Render_ShouldRenderNothing_WhenTextIsBlankAndCannotRefresh(string text)
    {
        // Arrange & Act
        var cut = _ctx.Render<DocumentLastUpdatedRow>(parameters => parameters
            .Add(p => p.LastUpdatedText, text)
            .Add(p => p.CanRefresh, false));

        // Assert
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render the last-updated text without a button when refresh is unavailable")]
    public void Render_ShouldShowTextOnly_WhenCannotRefresh()
    {
        // Arrange
        AuthorizeWithRefreshPermission();

        // Act
        var cut = _ctx.Render<DocumentLastUpdatedRow>(parameters => parameters
            .Add(p => p.LastUpdatedText, "Last updated: January 15, 2026")
            .Add(p => p.CanRefresh, false));

        // Assert
        cut.Find(".neba-document-toc-last-updated .updated-text").TextContent.ShouldBe("Last updated: January 15, 2026");
        cut.FindAll("button").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render the refresh button without text when there is no last-updated text")]
    public void Render_ShouldShowButtonOnly_WhenNoText()
    {
        // Arrange
        AuthorizeWithRefreshPermission();

        // Act
        var cut = _ctx.Render<DocumentLastUpdatedRow>(parameters => parameters
            .Add(p => p.LastUpdatedText, null)
            .Add(p => p.CanRefresh, true));

        // Assert
        cut.FindAll(".updated-text").ShouldBeEmpty();
        cut.FindAll(RefreshButtonSelector).Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should render both the text and the refresh button when both apply")]
    public void Render_ShouldShowTextAndButton_WhenBothApply()
    {
        // Arrange
        AuthorizeWithRefreshPermission();

        // Act
        var cut = _ctx.Render<DocumentLastUpdatedRow>(parameters => parameters
            .Add(p => p.LastUpdatedText, "Last updated: January 15, 2026")
            .Add(p => p.CanRefresh, true));

        // Assert
        cut.Find(".updated-text").TextContent.ShouldBe("Last updated: January 15, 2026");
        cut.Find(RefreshButtonSelector).ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should not render the refresh button when the caller lacks the RefreshDocument permission")]
    public void Render_ShouldHideButton_WhenCallerLacksPermission()
    {
        // Arrange
        _ctx.AddAuthorization().SetAuthorized("test-user");

        // Act
        var cut = _ctx.Render<DocumentLastUpdatedRow>(parameters => parameters
            .Add(p => p.LastUpdatedText, "Last updated: January 15, 2026")
            .Add(p => p.CanRefresh, true));

        // Assert
        cut.FindAll(RefreshButtonSelector).ShouldBeEmpty();
        cut.Find(".updated-text").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should not render the refresh button for an anonymous caller")]
    public void Render_ShouldHideButton_WhenCallerIsAnonymous()
    {
        // Arrange
        _ctx.AddAuthorization();

        // Act
        var cut = _ctx.Render<DocumentLastUpdatedRow>(parameters => parameters
            .Add(p => p.CanRefresh, true));

        // Assert
        cut.FindAll(RefreshButtonSelector).ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should label the button Refresh and leave it enabled when not refreshing")]
    public void Render_ShouldShowRefreshLabelAndEnableButton_WhenNotRefreshing()
    {
        // Arrange
        AuthorizeWithRefreshPermission();

        // Act
        var cut = _ctx.Render<DocumentLastUpdatedRow>(parameters => parameters
            .Add(p => p.CanRefresh, true)
            .Add(p => p.IsRefreshing, false));

        // Assert
        var button = cut.Find(RefreshButtonSelector);
        button.TextContent.Trim().ShouldBe("Refresh");
        button.HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact(DisplayName = "Should label the button Refreshing... and disable it while refreshing")]
    public void Render_ShouldShowRefreshingLabelAndDisableButton_WhenRefreshing()
    {
        // Arrange
        AuthorizeWithRefreshPermission();

        // Act
        var cut = _ctx.Render<DocumentLastUpdatedRow>(parameters => parameters
            .Add(p => p.CanRefresh, true)
            .Add(p => p.IsRefreshing, true));

        // Assert
        var button = cut.Find(RefreshButtonSelector);
        button.TextContent.Trim().ShouldBe("Refreshing...");
        button.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact(DisplayName = "Should invoke OnRefresh when the refresh button is clicked")]
    public void Click_ShouldInvokeOnRefresh_WhenRefreshButtonClicked()
    {
        // Arrange
        AuthorizeWithRefreshPermission();
        var refreshCount = 0;

        var cut = _ctx.Render<DocumentLastUpdatedRow>(parameters => parameters
            .Add(p => p.CanRefresh, true)
            .Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => refreshCount++)));

        // Act
        cut.Find(RefreshButtonSelector).Click();

        // Assert
        refreshCount.ShouldBe(1);
    }
}
