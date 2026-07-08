using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts;
using Neba.Api.Contracts.News;
using Neba.Api.Contracts.News.ListArticles;
using Neba.Api.Contracts.Security;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.News;
using Neba.Website.Server.Clock;
using Neba.Website.Server.News;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.News;

[UnitTest]
[Component("Website.News.NewsList")]
public sealed class NewsListTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<INewsApi> _mockApi;
    private readonly BunitAuthorizationContext _authContext;

    public NewsListTests()
    {
        _mockApi = new Mock<INewsApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _authContext = _ctx.AddAuthorization();
        _authContext.SetNotAuthorized();

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
            .Setup(x => x.ListArticlesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<PaginationResponse<ArticleSummaryResponse>>>().Task);

        // Act
        var cut = _ctx.Render<NewsList>();

        // Assert
        cut.Markup.ShouldContain("aria-busy=\"true\"");
    }

    [Fact(DisplayName = "Should show hero skeleton on page 1 while loading")]
    public void Render_ShouldShowHeroSkeleton_OnPageOneWhileLoading()
    {
        // Arrange
        _mockApi
            .Setup(x => x.ListArticlesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<PaginationResponse<ArticleSummaryResponse>>>().Task);

        // Act — no navigation = page 1 by default
        var cut = _ctx.Render<NewsList>();

        // Assert
        cut.Markup.ShouldContain("news-hero-skeleton");
    }

    [Fact(DisplayName = "Should not show hero skeleton when loading page 2")]
    public void Render_ShouldNotShowHeroSkeleton_WhenLoadingPageTwo()
    {
        // Arrange
        _mockApi
            .Setup(x => x.ListArticlesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<PaginationResponse<ArticleSummaryResponse>>>().Task);

        NavigateToPage(2);

        // Act
        var cut = _ctx.Render<NewsList>();

        // Assert
        cut.Markup.ShouldNotContain("news-hero-skeleton");
    }

    // ── Error state ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show error alert when API call fails")]
    public void Render_ShouldShowErrorAlert_WhenApiFails()
    {
        // Arrange
        SetupFailureResponse(System.Net.HttpStatusCode.InternalServerError);

        // Act
        var cut = _ctx.Render<NewsList>();

        // Assert
        cut.Markup.ShouldContain("Error Loading Articles");
    }

    // ── Empty state ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show empty state message when no articles exist")]
    public void Render_ShouldShowEmptyState_WhenNoArticlesExist()
    {
        // Arrange
        SetupSuccessResponse([], totalItems: 0);

        // Act
        var cut = _ctx.Render<NewsList>();

        // Assert
        cut.Markup.ShouldContain("No articles published yet");
    }

    // ── Page 1: hero + grid ──────────────────────────────────────────────────

    [Fact(DisplayName = "Should show hero with first article title on page 1")]
    public void Render_ShouldShowHeroWithFirstArticle_OnPageOne()
    {
        // Arrange
        var articles = ArticleSummaryResponseFactory.Bogus(4, 1);
        SetupSuccessResponse(articles, totalItems: 4);

        // Act
        var cut = _ctx.Render<NewsList>();

        // Assert
        cut.Markup.ShouldContain("news-hero");
        cut.Markup.ShouldContain(articles.First().Title);
    }

    [Fact(DisplayName = "Should link hero to first article's slug")]
    public void Render_ShouldLinkHero_ToFirstArticleSlug()
    {
        // Arrange
        var hero = ArticleSummaryResponseFactory.Create(slug: "season-champions-2026");
        var others = ArticleSummaryResponseFactory.Bogus(2, 42);
        SetupSuccessResponse([hero, .. others], totalItems: 3);

        // Act
        var cut = _ctx.Render<NewsList>();

        // Assert
        cut.Markup.ShouldContain("/news/season-champions-2026");
    }

    [Fact(DisplayName = "Should show grid with remaining articles after hero on page 1")]
    public void Render_ShouldShowGrid_WithRemainingArticlesAfterHero_OnPageOne()
    {
        // Arrange
        var articles = ArticleSummaryResponseFactory.Bogus(4, 7);
        SetupSuccessResponse(articles, totalItems: 4);

        // Act
        var cut = _ctx.Render<NewsList>();

        // Assert
        cut.Markup.ShouldContain("news-grid");
        foreach (var article in articles.Skip(1))
        {
            cut.Markup.ShouldContain(System.Net.WebUtility.HtmlEncode(article.Title));
        }
    }

    [Fact(DisplayName = "Should not show grid when only one article exists on page 1")]
    public void Render_ShouldNotShowGrid_WhenOnlyOneArticleExistsOnPageOne()
    {
        // Arrange
        var article = ArticleSummaryResponseFactory.Create();
        SetupSuccessResponse([article], totalItems: 1);

        // Act
        var cut = _ctx.Render<NewsList>();

        // Assert
        cut.Markup.ShouldContain("news-hero");
        cut.Markup.ShouldNotContain("news-grid");
    }

    // ── Page 2+: grid only ───────────────────────────────────────────────────

    [Fact(DisplayName = "Should not show hero on page 2")]
    public void Render_ShouldNotShowHero_OnPageTwo()
    {
        // Arrange
        var articles = ArticleSummaryResponseFactory.Bogus(3, 99);
        SetupSuccessResponse(articles, totalItems: 13, pageNumber: 2);
        NavigateToPage(2);

        // Act
        var cut = _ctx.Render<NewsList>();

        // Assert
        cut.Markup.ShouldNotContain("news-hero");
        cut.Markup.ShouldContain("news-grid");
    }

    [Fact(DisplayName = "Should show all articles in grid on page 2")]
    public void Render_ShouldShowAllArticlesInGrid_OnPageTwo()
    {
        // Arrange
        var articles = ArticleSummaryResponseFactory.Bogus(3, 55);
        SetupSuccessResponse(articles, totalItems: 13, pageNumber: 2);
        NavigateToPage(2);

        // Act
        var cut = _ctx.Render<NewsList>();

        // Assert
        foreach (var article in articles)
        {
            cut.Markup.ShouldContain(article.Title);
        }
    }

    // ── Pagination ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show pagination when there are multiple pages")]
    public void Render_ShouldShowPagination_WhenMultiplePagesExist()
    {
        // Arrange
        var articles = ArticleSummaryResponseFactory.Bogus(10, 5);
        SetupSuccessResponse(articles, totalItems: 25);

        // Act
        var cut = _ctx.Render<NewsList>();

        // Assert
        cut.Markup.ShouldContain("news-pagination");
    }

    [Fact(DisplayName = "Should not show pagination when only one page of articles exists")]
    public void Render_ShouldNotShowPagination_WhenOnlyOnePage()
    {
        // Arrange
        var articles = ArticleSummaryResponseFactory.Bogus(3, 12);
        SetupSuccessResponse(articles, totalItems: 3);

        // Act
        var cut = _ctx.Render<NewsList>();

        // Assert
        cut.Markup.ShouldNotContain("news-pagination");
    }

    [Fact(DisplayName = "Should mark current page button as active in pagination")]
    public void Render_ShouldMarkCurrentPage_AsActiveInPagination()
    {
        // Arrange
        var articles = ArticleSummaryResponseFactory.Bogus(10, 3);
        SetupSuccessResponse(articles, totalItems: 20, pageNumber: 2);
        NavigateToPage(2);

        // Act
        var cut = _ctx.Render<NewsList>();

        // Assert
        cut.Markup.ShouldContain("aria-current=\"page\"");
    }

    // ── API call ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should call API with page 1 when no page parameter is provided")]
    public void OnInit_ShouldCallApi_WithPageOne_WhenNoPageParameter()
    {
        // Arrange
        SetupSuccessResponse([], totalItems: 0);

        // Act
        _ctx.Render<NewsList>();

        // Assert
        _mockApi.Verify(
            x => x.ListArticlesAsync(1, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Should call API with specified page number")]
    public void OnInit_ShouldCallApi_WithSpecifiedPage()
    {
        // Arrange
        SetupSuccessResponse([], totalItems: 0, pageNumber: 3);
        NavigateToPage(3);

        // Act
        _ctx.Render<NewsList>();

        // Assert
        _mockApi.Verify(
            x => x.ListArticlesAsync(3, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Delete flow ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should not show delete icon on cards when user lacks DeleteArticle permission")]
    public void Render_ShouldNotShowDeleteIcon_WhenUserLacksPermission()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        var articles = ArticleSummaryResponseFactory.Bogus(2, 21);
        SetupSuccessResponse(articles, totalItems: 2, pageNumber: 2);
        NavigateToPage(2);

        // Act
        var cut = _ctx.Render<NewsList>();

        // Assert
        cut.FindAll("button.icon-btn").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show delete icon on cards when user has DeleteArticle permission")]
    public void Render_ShouldShowDeleteIcon_WhenUserHasPermission()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.DeleteArticle.PolicyName);
        var articles = ArticleSummaryResponseFactory.Bogus(2, 22);
        SetupSuccessResponse(articles, totalItems: 2, pageNumber: 2);
        NavigateToPage(2);

        // Act
        var cut = _ctx.Render<NewsList>();

        // Assert
        cut.FindAll("button.icon-btn").Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Should open confirm dialog naming the article when delete icon is clicked")]
    public void Click_ShouldOpenConfirmDialog_WhenDeleteIconIsClicked()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.DeleteArticle.PolicyName);
        var articles = ArticleSummaryResponseFactory.Bogus(1, 23);
        SetupSuccessResponse(articles, totalItems: 1, pageNumber: 2);
        NavigateToPage(2);
        var cut = _ctx.Render<NewsList>();

        // Act
        cut.Find("button.icon-btn").Click();

        // Assert
        cut.Markup.ShouldContain("Delete article?");
        cut.Markup.ShouldContain(System.Net.WebUtility.HtmlEncode(articles.Single().Title));
    }

    [Fact(DisplayName = "Should remove article from grid when delete succeeds")]
    public void ConfirmDelete_ShouldRemoveArticleFromGrid_WhenDeleteSucceeds()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.DeleteArticle.PolicyName);
        var articles = ArticleSummaryResponseFactory.Bogus(2, 24);
        var deletedArticle = articles.First();
        SetupSuccessResponse(articles, totalItems: 2, pageNumber: 2);

        using var deleteResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.NoContent
        };
        _mockApi
            .Setup(x => x.DeleteArticleAsync(deletedArticle.ArticleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteResponse);

        NavigateToPage(2);
        var cut = _ctx.Render<NewsList>();
        cut.FindAll("button.icon-btn")[0].Click();

        // Act
        cut.Find("button.confirm-action-modal-confirm").Click();

        // Assert
        cut.Markup.ShouldNotContain(System.Net.WebUtility.HtmlEncode(deletedArticle.Title));
    }

    [Fact(DisplayName = "Should show error alert and keep article in grid when delete fails")]
    public void ConfirmDelete_ShouldShowErrorAlert_WhenDeleteFails()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.DeleteArticle.PolicyName);
        var articles = ArticleSummaryResponseFactory.Bogus(1, 25);
        var targetArticle = articles.Single();
        SetupSuccessResponse(articles, totalItems: 1, pageNumber: 2);

        using var deleteResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = false,
            StatusCode = System.Net.HttpStatusCode.Forbidden
        };
        _mockApi
            .Setup(x => x.DeleteArticleAsync(targetArticle.ArticleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteResponse);

        NavigateToPage(2);
        var cut = _ctx.Render<NewsList>();
        cut.Find("button.icon-btn").Click();

        // Act
        cut.Find("button.confirm-action-modal-confirm").Click();

        // Assert
        cut.Markup.ShouldContain("Error Loading Articles");
        cut.Markup.ShouldContain(System.Net.WebUtility.HtmlEncode(targetArticle.Title));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void NavigateToPage(int page)
        => _ctx.Services
            .GetRequiredService<NavigationManager>()
            .NavigateTo($"http://localhost/news?page={page}");

    private void SetupSuccessResponse(
        IReadOnlyCollection<ArticleSummaryResponse> articles,
        int totalItems,
        int pageNumber = 1)
    {
        using var response = new StubApiResponse<PaginationResponse<ArticleSummaryResponse>>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new PaginationResponse<ArticleSummaryResponse>
            {
                Items = articles,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = 10,
            }
        };

        _mockApi
            .Setup(x => x.ListArticlesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupFailureResponse(System.Net.HttpStatusCode statusCode)
    {
        using var response = new StubApiResponse<PaginationResponse<ArticleSummaryResponse>>
        {
            IsSuccessStatusCode = false,
            StatusCode = statusCode,
            Content = (PaginationResponse<ArticleSummaryResponse>?)null
        };

        _mockApi
            .Setup(x => x.ListArticlesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }
}