using FastEndpoints;

using Neba.Api.Features.News.ListArticles;
using Neba.Api.Messaging;
using Neba.TestFactory;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.News;

namespace Neba.Api.Tests.Features.News.ListArticles;

[UnitTest]
[Component("News")]
public sealed class ListArticlesEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped articles and pagination when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedArticlesAndPagination_WhenQuerySucceeds()
    {
        // Arrange
        var dtos = ArticleSummaryDtoFactory.Bogus(3, 42);
        var pagedResult = dtos.WithTotalItems(15);
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListArticlesQuery, PagedResult<ArticleSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ListArticlesQuery>(), cancellationToken))
            .ReturnsAsync(pagedResult);

        var endpoint = Factory.Create<ListArticlesEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new ListArticlesRequest { Page = 2, PageSize = 10 }, cancellationToken);

        // Assert
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should return OK with empty items when no articles exist")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyItems_WhenNoArticlesExist()
    {
        // Arrange
        var pagedResult = new PagedResult<ArticleSummaryDto>([], 0);
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListArticlesQuery, PagedResult<ArticleSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ListArticlesQuery>(), cancellationToken))
            .ReturnsAsync(pagedResult);

        var endpoint = Factory.Create<ListArticlesEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new ListArticlesRequest { Page = 1, PageSize = 10 }, cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Items.ShouldBeEmpty();
        endpoint.Response.TotalItems.ShouldBe(0);
        endpoint.Response.TotalPages.ShouldBe(0);
        endpoint.Response.HasNextPage.ShouldBeFalse();
        endpoint.Response.HasPreviousPage.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync should pass page and pageSize from request to query")]
    public async Task HandleAsync_ShouldPassPageAndPageSize_FromRequestToQuery()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        ListArticlesQuery? capturedQuery = null;

        var queryHandlerMock = new Mock<IQueryHandler<ListArticlesQuery, PagedResult<ArticleSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ListArticlesQuery>(), cancellationToken))
            .Callback<ListArticlesQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync(new PagedResult<ArticleSummaryDto>([], 0));

        var endpoint = Factory.Create<ListArticlesEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new ListArticlesRequest { Page = 3, PageSize = 5 }, cancellationToken);

        // Assert
        capturedQuery.ShouldNotBeNull();
        capturedQuery.Page.ShouldBe(3);
        capturedQuery.PageSize.ShouldBe(5);
        endpoint.Response.PageNumber.ShouldBe(3);
        endpoint.Response.PageSize.ShouldBe(5);
    }

    [Fact(DisplayName = "Configure should register anonymous GET route under /news")]
    public void Configure_ShouldRegisterAnonymousGetRoute_UnderNewsPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListArticlesQuery, PagedResult<ArticleSummaryDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListArticlesEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("news"), "should be under the /news path");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}