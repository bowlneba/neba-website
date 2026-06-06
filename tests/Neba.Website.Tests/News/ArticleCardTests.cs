using Bunit;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.News;
using Neba.Website.Server.News;

namespace Neba.Website.Tests.News;

[UnitTest]
[Component("Website.News.ArticleCard")]
public sealed class ArticleCardTests : IDisposable
{
    private readonly BunitContext _ctx;

    public ArticleCardTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
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
}