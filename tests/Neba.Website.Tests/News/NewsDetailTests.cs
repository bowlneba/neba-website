using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.News;
using Neba.Api.Contracts.News.GetArticle;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.News;
using Neba.Website.Server.Clock;
using Neba.Website.Server.News;
using Neba.Website.Server.Services;

using Refit;

namespace Neba.Website.Tests.News;

[UnitTest]
[Component("Website.News.NewsDetail")]
public sealed class NewsDetailTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<INewsApi> _mockApi;

    public NewsDetailTests()
    {
        _mockApi = new Mock<INewsApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose() => _ctx.Dispose();

    // ── Loading state ────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show loading skeleton while API is pending")]
    public void Render_ShouldShowLoadingSkeleton_WhileLoading()
    {
        // Arrange
        _mockApi
            .Setup(x => x.GetArticleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<ArticleDetailResponse>>().Task);

        // Act
        var cut = _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, "any-slug"));

        // Assert
        cut.Markup.ShouldContain("aria-busy=\"true\"");
    }

    // ── Error state ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show error alert when API returns a server error")]
    public void Render_ShouldShowErrorAlert_WhenApiReturnsServerError()
    {
        // Arrange
        SetupFailureResponse(System.Net.HttpStatusCode.InternalServerError);

        // Act
        var cut = _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, "any-slug"));

        // Assert
        cut.Markup.ShouldContain("Error Loading Article");
    }

    // ── Not found state ──────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show not-found message when API returns 404")]
    public void Render_ShouldShowNotFoundMessage_WhenApiReturnsNotFound()
    {
        // Arrange
        SetupFailureResponse(System.Net.HttpStatusCode.NotFound);

        // Act
        var cut = _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, "missing-slug"));

        // Assert
        cut.Markup.ShouldContain("could not be found");
    }

    [Fact(DisplayName = "Should show back-to-news link in not-found state")]
    public void Render_ShouldShowBackToNewsLink_InNotFoundState()
    {
        // Arrange
        SetupFailureResponse(System.Net.HttpStatusCode.NotFound);

        // Act
        var cut = _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, "missing-slug"));

        // Assert
        cut.Markup.ShouldContain("Back to News");
        cut.Markup.ShouldContain("href=\"/news\"");
    }

    // ── Article content ──────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render article title")]
    public void Render_ShouldShowArticleTitle_WhenLoaded()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create(title: "Season Champions Crowned");
        SetupSuccessResponse(article);

        // Act
        var cut = _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, article.Slug));

        // Assert
        cut.Markup.ShouldContain("Season Champions Crowned");
    }

    [Fact(DisplayName = "Should render article content as HTML")]
    public void Render_ShouldRenderArticleContent_AsHtml()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create(content: "<p>The bowlers were ready.</p>");
        SetupSuccessResponse(article);

        // Act
        var cut = _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, article.Slug));

        // Assert
        cut.Markup.ShouldContain("The bowlers were ready.");
    }

    [Fact(DisplayName = "Should format publish date as full month, day, and year")]
    public void Render_ShouldFormatPublishDate_AsFullMonthDayYear()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create(
            publishDateUtc: new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero));
        SetupSuccessResponse(article);

        // Act
        var cut = _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, article.Slug));

        // Assert
        cut.Markup.ShouldContain("May 15, 2026");
    }

    // ── Header image ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render header image when HeaderImageUrl is set")]
    public void Render_ShouldShowHeaderImage_WhenHeaderImageUrlIsSet()
    {
        // Arrange
        var imageUrl = new Uri("https://files.bowlneba.com/news/article-1/header.jpg");
        var article = ArticleDetailResponseFactory.Create(headerImageUrl: imageUrl);
        SetupSuccessResponse(article);

        // Act
        var cut = _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, article.Slug));

        // Assert
        cut.Find("img").ShouldNotBeNull();
        cut.Markup.ShouldContain("https://files.bowlneba.com/news/article-1/header.jpg");
    }

    [Fact(DisplayName = "Should not render image element when HeaderImageUrl is null")]
    public void Render_ShouldNotShowImg_WhenHeaderImageUrlIsNull()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create(headerImageUrl: null);
        SetupSuccessResponse(article);

        // Act
        var cut = _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, article.Slug));

        // Assert
        cut.FindAll("img").ShouldBeEmpty();
    }

    // ── Back to news links ───────────────────────────────────────────────────

    [Fact(DisplayName = "Should show back-to-news links when article is loaded")]
    public void Render_ShouldShowBackToNewsLinks_WhenArticleIsLoaded()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create();
        SetupSuccessResponse(article);

        // Act
        var cut = _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, article.Slug));

        // Assert
        var backLinks = cut.FindAll("a[href='/news']");
        backLinks.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    // ── Sidebar: Article Info ─────────────────────────────────────────────────

    [Fact(DisplayName = "Should show publish date in sidebar Article Info panel")]
    public void Render_ShouldShowPublishDateInSidebar_WhenArticleIsLoaded()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create(
            publishDateUtc: new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero));
        SetupSuccessResponse(article);

        // Act
        var cut = _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, article.Slug));

        // Assert
        cut.Markup.ShouldContain("Article Info");
        cut.Markup.ShouldContain("March 10, 2026");
    }

    // ── Sidebar: Tournament link ──────────────────────────────────────────────

    [Fact(DisplayName = "Should show Go to Tournament link when TournamentId is set")]
    public void Render_ShouldShowTournamentLink_WhenTournamentIdIsSet()
    {
        // Arrange
        var tournamentId = "01JX0000000000000000000010";
        var article = ArticleDetailResponseFactory.Create(tournamentId: tournamentId);
        SetupSuccessResponse(article);

        // Act
        var cut = _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, article.Slug));

        // Assert
        cut.Markup.ShouldContain("Go to Tournament");
        cut.Markup.ShouldContain($"/tournaments/{tournamentId}");
    }

    [Fact(DisplayName = "Should not show Go to Tournament link when TournamentId is null")]
    public void Render_ShouldNotShowTournamentLink_WhenTournamentIdIsNull()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create(tournamentId: null);
        SetupSuccessResponse(article);

        // Act
        var cut = _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, article.Slug));

        // Assert
        cut.Markup.ShouldNotContain("Go to Tournament");
    }

    // ── Sidebar: Attached files ───────────────────────────────────────────────

    [Fact(DisplayName = "Should show Attached Files panel when article has attachments")]
    public void Render_ShouldShowAttachedFilesPanel_WhenArticleHasAttachments()
    {
        // Arrange
        var attachments = new[]
        {
            ArticleAttachmentResponseFactory.Create(
                displayName: "Tournament Results",
                url: new Uri("https://files.bowlneba.com/news/results.pdf")),
        };
        var article = ArticleDetailResponseFactory.Create(attachments: attachments);
        SetupSuccessResponse(article);

        // Act
        var cut = _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, article.Slug));

        // Assert
        cut.Markup.ShouldContain("Attached Files");
        cut.Markup.ShouldContain("Tournament Results");
    }

    [Fact(DisplayName = "Should not show Attached Files panel when article has no attachments")]
    public void Render_ShouldNotShowAttachedFilesPanel_WhenArticleHasNoAttachments()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create(attachments: []);
        SetupSuccessResponse(article);

        // Act
        var cut = _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, article.Slug));

        // Assert
        cut.Markup.ShouldNotContain("Attached Files");
    }

    [Fact(DisplayName = "Should render download link for attachment when URL is set")]
    public void Render_ShouldRenderDownloadLink_WhenAttachmentHasUrl()
    {
        // Arrange
        var fileUrl = new Uri("https://files.bowlneba.com/news/bracket.pdf");
        var attachments = new[]
        {
            ArticleAttachmentResponseFactory.Create(displayName: "Finals Bracket", url: fileUrl),
        };
        var article = ArticleDetailResponseFactory.Create(attachments: attachments);
        SetupSuccessResponse(article);

        // Act
        var cut = _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, article.Slug));

        // Assert
        cut.Markup.ShouldContain("https://files.bowlneba.com/news/bracket.pdf");
        cut.Markup.ShouldContain("Finals Bracket");
    }

    [Fact(DisplayName = "Should show unavailable label when attachment URL is null")]
    public void Render_ShouldShowUnavailableLabel_WhenAttachmentUrlIsNull()
    {
        // Arrange
        var attachments = new[]
        {
            ArticleAttachmentResponseFactory.Create(displayName: "Qualifying Scores", url: null),
        };
        var article = ArticleDetailResponseFactory.Create(attachments: attachments);
        SetupSuccessResponse(article);

        // Act
        var cut = _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, article.Slug));

        // Assert
        cut.Markup.ShouldContain("Qualifying Scores");
        cut.Markup.ShouldContain("Unavailable");
    }

    // ── API call ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should call API with the slug parameter")]
    public void OnInit_ShouldCallApi_WithSlugParameter()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create(slug: "season-recap-2026");
        SetupSuccessResponse(article);

        // Act
        _ctx.Render<NewsDetail>(p => p.Add(x => x.Slug, "season-recap-2026"));

        // Assert
        _mockApi.Verify(
            x => x.GetArticleAsync("season-recap-2026", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetupSuccessResponse(ArticleDetailResponse article)
    {
        var response = new Mock<IApiResponse<ArticleDetailResponse>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(true);
        response.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);
        response.Setup(r => r.Content).Returns(article);

        _mockApi
            .Setup(x => x.GetArticleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }

    private void SetupFailureResponse(System.Net.HttpStatusCode statusCode)
    {
        var response = new Mock<IApiResponse<ArticleDetailResponse>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(false);
        response.Setup(r => r.StatusCode).Returns(statusCode);
        response.Setup(r => r.Content).Returns((ArticleDetailResponse?)null);

        _mockApi
            .Setup(x => x.GetArticleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }
}
