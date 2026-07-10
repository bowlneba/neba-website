using Bunit;
using Bunit.TestDoubles;

using Neba.Api.Contracts.News.ListArticles;
using Neba.Api.Contracts.Security;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.News;
using Neba.Website.Server.News;

namespace Neba.Website.Tests.News;

[UnitTest]
[Component("Website.News.ArticleCard")]
public sealed class ArticleCardTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly BunitAuthorizationContext _authContext;

    public ArticleCardTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _authContext = _ctx.AddAuthorization();
        _authContext.SetNotAuthorized();
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render article title")]
    public void Render_ShouldShowArticleTitle_WhenRendered()
    {
        // Arrange
        var article = ArticleSummaryResponseFactory.Create(title: "Champions Crowned at Tournament of Champions");

        // Act
        var cut = _ctx.Render<ArticleCard>(p => p.Add(x => x.Article, article));

        // Assert
        cut.Markup.ShouldContain("Champions Crowned at Tournament of Champions");
    }

    [Fact(DisplayName = "Should render article excerpt")]
    public void Render_ShouldShowArticleExcerpt_WhenRendered()
    {
        // Arrange
        var article = ArticleSummaryResponseFactory.Create(excerpt: "A short preview of the article body.");

        // Act
        var cut = _ctx.Render<ArticleCard>(p => p.Add(x => x.Article, article));

        // Assert
        cut.Markup.ShouldContain("A short preview of the article body.");
    }

    [Fact(DisplayName = "Should format publish date as abbreviated month, day, and year")]
    public void Render_ShouldFormatPublishDate_AsAbbreviatedMonthDayYear()
    {
        // Arrange
        var article = ArticleSummaryResponseFactory.Create(
            publishDateUtc: new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero));

        // Act
        var cut = _ctx.Render<ArticleCard>(p => p.Add(x => x.Article, article));

        // Assert
        cut.Markup.ShouldContain("May 15, 2026");
    }

    [Fact(DisplayName = "Should format LocalPublishDate instead of Article.PublishDateUtc when supplied")]
    public void Render_ShouldFormatLocalPublishDate_WhenSupplied()
    {
        // Arrange
        // The parent page (NewsList) resolves the viewer's local time and passes it down,
        // and the card must display that instead of recomputing anything from the raw UTC value.
        var article = ArticleSummaryResponseFactory.Create(
            publishDateUtc: new DateTimeOffset(2026, 5, 15, 2, 0, 0, TimeSpan.Zero));
        var localPublishDate = new DateTimeOffset(2026, 5, 14, 22, 0, 0, TimeSpan.FromHours(-4));

        // Act
        var cut = _ctx.Render<ArticleCard>(p => p
            .Add(x => x.Article, article)
            .Add(x => x.LocalPublishDate, localPublishDate));

        // Assert
        cut.Markup.ShouldContain("May 14, 2026");
        cut.Markup.ShouldNotContain("May 15, 2026");
    }

    [Fact(DisplayName = "Should link to /news/{slug}")]
    public void Render_ShouldLinkToArticleDetailPage_UsingSlug()
    {
        // Arrange
        var article = ArticleSummaryResponseFactory.Create(slug: "champions-crowned");

        // Act
        var cut = _ctx.Render<ArticleCard>(p => p.Add(x => x.Article, article));

        // Assert
        cut.Markup.ShouldContain("/news/champions-crowned");
    }

    [Fact(DisplayName = "Should render header image when HeaderImageUrl is set")]
    public void Render_ShouldShowHeaderImage_WhenHeaderImageUrlIsSet()
    {
        // Arrange
        var imageUrl = new Uri("https://files.bowlneba.com/news/article-1/header.jpg");
        var article = ArticleSummaryResponseFactory.Create(headerImageUrl: imageUrl);

        // Act
        var cut = _ctx.Render<ArticleCard>(p => p.Add(x => x.Article, article));

        // Assert
        cut.Markup.ShouldContain("https://files.bowlneba.com/news/article-1/header.jpg");
        cut.Find("img").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should not render img element when HeaderImageUrl is null")]
    public void Render_ShouldNotShowImg_WhenHeaderImageUrlIsNull()
    {
        // Arrange
        var article = ArticleSummaryResponseFactory.Create(headerImageUrl: null);

        // Act
        var cut = _ctx.Render<ArticleCard>(p => p.Add(x => x.Article, article));

        // Assert
        cut.FindAll("img").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should not render delete button when user lacks DeleteArticle permission")]
    public void Render_ShouldNotShowDeleteButton_WhenUserLacksPermission()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        var article = ArticleSummaryResponseFactory.Create();

        // Act
        var cut = _ctx.Render<ArticleCard>(p => p.Add(x => x.Article, article));

        // Assert
        cut.FindAll("button.icon-btn").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render delete button when user has DeleteArticle permission")]
    public void Render_ShouldShowDeleteButton_WhenUserHasPermission()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.DeleteArticle.PolicyName);
        var article = ArticleSummaryResponseFactory.Create();

        // Act
        var cut = _ctx.Render<ArticleCard>(p => p.Add(x => x.Article, article));

        // Assert
        cut.Find("button.icon-btn").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should not render status badge when user lacks CanManageArticles permission")]
    public void Render_ShouldNotShowStatusBadge_WhenUserLacksPermission()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        var article = ArticleSummaryResponseFactory.Create();

        // Act
        var cut = _ctx.Render<ArticleCard>(p => p.Add(x => x.Article, article));

        // Assert
        cut.FindAll("span.article-status-badge").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render status badge when user has CanManageArticles permission")]
    public void Render_ShouldShowStatusBadge_WhenUserHasPermission()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.CanManageArticlesPolicyName);
        var article = ArticleSummaryResponseFactory.Create();

        // Act
        var cut = _ctx.Render<ArticleCard>(p => p.Add(x => x.Article, article));

        // Assert
        cut.Find("span.article-status-badge").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should invoke OnDeleteRequested with the article when delete button is clicked")]
    public void Click_ShouldInvokeOnDeleteRequestedWithArticle_WhenDeleteButtonIsClicked()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.DeleteArticle.PolicyName);
        var article = ArticleSummaryResponseFactory.Create();
        ArticleSummaryResponse? requested = null;

        var cut = _ctx.Render<ArticleCard>(p => p
            .Add(x => x.Article, article)
            .Add(x => x.OnDeleteRequested, a => requested = a));

        // Act
        cut.Find("button.icon-btn").Click();

        // Assert
        requested.ShouldBe(article);
    }
}