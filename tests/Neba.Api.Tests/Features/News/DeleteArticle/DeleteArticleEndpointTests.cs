using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.News.DeleteArticle;
using Neba.Api.Features.News.Domain;
using Neba.TestFactory.Attributes;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.News.DeleteArticle;

[UnitTest]
[Component("News")]
public sealed class DeleteArticleEndpointTests
{
    private const string ValidArticleId = "01000000000000000000000001";

    [Fact(DisplayName = "HandleAsync should return 204 NoContent when article is deleted")]
    public async Task HandleAsync_ShouldReturn204_WhenArticleIsDeleted()
    {
        // Arrange
        var request = new DeleteArticleRequest { Id = ValidArticleId };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<DeleteArticleCommand, Deleted>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<DeleteArticleCommand>(), ct))
            .ReturnsAsync(Result.Deleted);

        var endpoint = Factory.Create<DeleteArticleEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
    }

    [Fact(DisplayName = "HandleAsync should map the request id to the command's ArticleId")]
    public async Task HandleAsync_ShouldMapRequestId_ToCommandArticleId()
    {
        // Arrange
        var request = new DeleteArticleRequest { Id = ValidArticleId };
        var ct = TestContext.Current.CancellationToken;
        DeleteArticleCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<DeleteArticleCommand, Deleted>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<DeleteArticleCommand>(), ct))
            .Callback<DeleteArticleCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(Result.Deleted);

        var endpoint = Factory.Create<DeleteArticleEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        capturedCommand.ShouldNotBeNull();
        capturedCommand.ArticleId.ShouldBe(new ArticleId(ValidArticleId));
    }

    [Fact(DisplayName = "Configure should register a permission-protected DELETE route under /news")]
    public void Configure_ShouldRegisterPermissionProtectedDeleteRoute_UnderNewsPath()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<DeleteArticleCommand, Deleted>>(MockBehavior.Strict);
        var endpoint = Factory.Create<DeleteArticleEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("DELETE");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("news"), "should be under the /news path");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}