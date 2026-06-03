using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.News.GetArticle;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.News;

namespace Neba.Api.Tests.Features.News.GetArticle;

[UnitTest]
[Component("News")]
public sealed class GetArticleEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped article when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedArticle_WhenQuerySucceeds()
    {
        // Arrange
        var dto = ArticleDetailDtoFactory.Bogus(1, 42).Single();
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<GetArticleQuery, ErrorOr<ArticleDetailDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetArticleQuery>(), cancellationToken))
            .ReturnsAsync(dto);

        var endpoint = Factory.Create<GetArticleEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetArticleRequest { Slug = dto.Slug }, cancellationToken);

        // Assert
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should return 404 when article is not found")]
    public async Task HandleAsync_ShouldReturn404_WhenArticleIsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<GetArticleQuery, ErrorOr<ArticleDetailDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetArticleQuery>(), cancellationToken))
            .ReturnsAsync(Error.NotFound());

        var endpoint = Factory.Create<GetArticleEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetArticleRequest { Slug = "nonexistent-slug" }, cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "HandleAsync should pass slug from request to query")]
    public async Task HandleAsync_ShouldPassSlug_FromRequestToQuery()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        GetArticleQuery? capturedQuery = null;

        var queryHandlerMock = new Mock<IQueryHandler<GetArticleQuery, ErrorOr<ArticleDetailDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetArticleQuery>(), cancellationToken))
            .Callback<GetArticleQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync(ArticleDetailDtoFactory.Create());

        var endpoint = Factory.Create<GetArticleEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetArticleRequest { Slug = "my-article-slug" }, cancellationToken);

        // Assert
        capturedQuery.ShouldNotBeNull();
        capturedQuery.Slug.ShouldBe("my-article-slug");
    }

    [Fact(DisplayName = "Configure should register anonymous GET route under /news")]
    public void Configure_ShouldRegisterAnonymousGetRoute_UnderNewsPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<GetArticleQuery, ErrorOr<ArticleDetailDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<GetArticleEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("news"), "should be under the /news path");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}
