using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.News.CreateArticle;
using Neba.Api.Features.News.CreateArticle;
using Neba.Api.Features.News.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.News;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.News.CreateArticle;

[UnitTest]
[Component("News")]
public sealed class CreateArticleEndpointTests
{
    [Fact(DisplayName = "HandleAsync should map request fields to command and take the success branch when creation succeeds")]
    public async Task HandleAsync_ShouldMapRequestToCommandAndTakeSuccessBranch_WhenCreationSucceeds()
    {
        // Arrange
        var input = ArticleInputFactory.Create(title: "My Title", slug: "my-slug");
        var request = new CreateArticleRequest { Article = input };
        var ct = TestContext.Current.CancellationToken;
        var createdArticle = CreatedArticleFactory.Create(slug: "my-slug");

        CreateArticleCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateArticleCommand, CreatedArticle>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateArticleCommand>(), ct))
            .Callback<CreateArticleCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(createdArticle);

        var endpoint = Factory.Create<CreateArticleEndpoint>(commandHandlerMock.Object);

        // Act — Send.CreatedAtAsync requires LinkGenerator, which Factory.Create does not provide.
        // The strict mock verifies the command mapping; the LinkGenerator exception confirms the success branch was taken.
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => endpoint.HandleAsync(request, ct));

        // Assert
        exception.Message.ShouldContain("LinkGenerator");
        capturedCommand.ShouldNotBeNull();
        capturedCommand.Title.ShouldBe(input.Title);
        capturedCommand.Slug.ShouldBe(input.Slug);
        capturedCommand.Content.ShouldBe(input.Content);
        capturedCommand.PublicationStatus.ShouldBe(PublicationStatus.FromName(input.PublicationStatus));
        capturedCommand.PublishDateUtc.ShouldBe(input.PublishDateUtc);
        capturedCommand.TournamentId.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should map a supplied TournamentId onto the command")]
    public async Task HandleAsync_ShouldMapTournamentId_WhenSupplied()
    {
        // Arrange
        var tournamentId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        var input = ArticleInputFactory.Create(tournamentId: tournamentId);
        var request = new CreateArticleRequest { Article = input };
        var ct = TestContext.Current.CancellationToken;
        var createdArticle = CreatedArticleFactory.Create();

        CreateArticleCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateArticleCommand, CreatedArticle>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateArticleCommand>(), ct))
            .Callback<CreateArticleCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(createdArticle);

        var endpoint = Factory.Create<CreateArticleEndpoint>(commandHandlerMock.Object);

        // Act
        await Should.ThrowAsync<InvalidOperationException>(
            () => endpoint.HandleAsync(request, ct));

        // Assert
        capturedCommand.ShouldNotBeNull();
        capturedCommand.TournamentId.ShouldBe(new Neba.Api.Features.Tournaments.Domain.TournamentId(tournamentId));
    }

    [Fact(DisplayName = "HandleAsync should return 409 Conflict when the command returns a conflict error")]
    public async Task HandleAsync_ShouldReturn409_WhenCommandReturnsConflictError()
    {
        // Arrange
        var request = new CreateArticleRequest { Article = ArticleInputFactory.Create() };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateArticleCommand, CreatedArticle>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateArticleCommand>(), ct))
            .ReturnsAsync(ArticleErrors.SlugAlreadyExists("my-slug"));

        var endpoint = Factory.Create<CreateArticleEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }

    [Fact(DisplayName = "HandleAsync should return 422 when the command returns validation errors")]
    public async Task HandleAsync_ShouldReturn422_WhenCommandReturnsValidationErrors()
    {
        // Arrange
        var request = new CreateArticleRequest { Article = ArticleInputFactory.Create() };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateArticleCommand, CreatedArticle>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateArticleCommand>(), ct))
            .ReturnsAsync(ArticleErrors.TitleRequired);

        var endpoint = Factory.Create<CreateArticleEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "Configure should register a permission-protected POST route under /news")]
    public void Configure_ShouldRegisterPermissionProtectedPostRoute_UnderNewsPath()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateArticleCommand, CreatedArticle>>(MockBehavior.Strict);
        var endpoint = Factory.Create<CreateArticleEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("news"), "should be under the /news path");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}
