using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.News.CreateArticle;
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.News.EditArticle;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.News;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.News.EditArticle;

[UnitTest]
[Component("News")]
public sealed class EditArticleEndpointTests
{
    private const string ValidArticleId = "01000000000000000000000001";

    [Fact(DisplayName = "HandleAsync should map request fields to command and take the success branch when the edit succeeds")]
    public async Task HandleAsync_ShouldMapRequestToCommandAndTakeSuccessBranch_WhenEditSucceeds()
    {
        // Arrange
        var input = EditArticleInputFactory.Create(title: "My Title");
        var request = new EditArticleRequest { Id = ValidArticleId, Article = input };
        var ct = TestContext.Current.CancellationToken;

        EditArticleCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditArticleCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditArticleCommand>(), ct))
            .Callback<EditArticleCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(Result.Updated);

        var endpoint = Factory.Create<EditArticleEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        capturedCommand.ShouldNotBeNull();
        capturedCommand.ArticleId.ShouldBe(new ArticleId(ValidArticleId));
        capturedCommand.Title.ShouldBe(input.Title);
        capturedCommand.Content.ShouldBe(input.Content);
        capturedCommand.PublicationStatus.ShouldBe(PublicationStatus.FromName(input.PublicationStatus));
        capturedCommand.PublishDate.ShouldBe(input.PublishDate);
        capturedCommand.TournamentId.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should map a supplied TournamentId onto the command")]
    public async Task HandleAsync_ShouldMapTournamentId_WhenSupplied()
    {
        // Arrange
        const string tournamentId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        var input = EditArticleInputFactory.Create(tournamentId: tournamentId);
        var request = new EditArticleRequest { Id = ValidArticleId, Article = input };
        var ct = TestContext.Current.CancellationToken;

        EditArticleCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditArticleCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditArticleCommand>(), ct))
            .Callback<EditArticleCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(Result.Updated);

        var endpoint = Factory.Create<EditArticleEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        capturedCommand.ShouldNotBeNull();
        capturedCommand.TournamentId.ShouldBe(new Neba.Api.Features.Tournaments.Domain.TournamentId(tournamentId));
    }

    [Fact(DisplayName = "HandleAsync should map a supplied header image and attachments onto the command")]
    public async Task HandleAsync_ShouldMapHeaderImageAndAttachments_WhenSupplied()
    {
        // Arrange
        var headerImage = new HeaderImageInput
        {
            Container = "news",
            Path = "uploads/header/01ARZ3-header.png",
            ContentType = "image/png",
            SizeInBytes = 2048
        };
        var attachment = new AttachmentInput
        {
            DisplayName = "Bracket.pdf",
            IsInline = false,
            Container = "news",
            Path = "uploads/attachments/01ARZ4-bracket.pdf",
            ContentType = "application/pdf",
            SizeInBytes = 4096
        };
        var input = EditArticleInputFactory.Create(headerImage: headerImage, attachments: [attachment]);
        var request = new EditArticleRequest { Id = ValidArticleId, Article = input };
        var ct = TestContext.Current.CancellationToken;

        EditArticleCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditArticleCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditArticleCommand>(), ct))
            .Callback<EditArticleCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(Result.Updated);

        var endpoint = Factory.Create<EditArticleEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        capturedCommand.ShouldNotBeNull();
        capturedCommand.HeaderImage.ShouldNotBeNull();
        capturedCommand.HeaderImage.Container.ShouldBe(headerImage.Container);
        capturedCommand.HeaderImage.Path.ShouldBe(headerImage.Path);
        capturedCommand.HeaderImage.ContentType.ShouldBe(headerImage.ContentType);
        capturedCommand.HeaderImage.SizeInBytes.ShouldBe(headerImage.SizeInBytes);

        capturedCommand.Attachments.ShouldHaveSingleItem();
        var capturedAttachment = capturedCommand.Attachments.Single();
        capturedAttachment.DisplayName.ShouldBe(attachment.DisplayName);
        capturedAttachment.IsInline.ShouldBe(attachment.IsInline);
        capturedAttachment.File.Container.ShouldBe(attachment.Container);
        capturedAttachment.File.Path.ShouldBe(attachment.Path);
        capturedAttachment.File.ContentType.ShouldBe(attachment.ContentType);
        capturedAttachment.File.SizeInBytes.ShouldBe(attachment.SizeInBytes);
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the command returns a not-found error")]
    public async Task HandleAsync_ShouldReturn404_WhenCommandReturnsNotFoundError()
    {
        // Arrange
        var request = new EditArticleRequest { Id = ValidArticleId, Article = EditArticleInputFactory.Create() };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditArticleCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditArticleCommand>(), ct))
            .ReturnsAsync(ArticleErrors.ArticleNotFound(ValidArticleId));

        var endpoint = Factory.Create<EditArticleEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "HandleAsync should return 409 Conflict when the command returns a conflict error")]
    public async Task HandleAsync_ShouldReturn409_WhenCommandReturnsConflictError()
    {
        // Arrange
        var request = new EditArticleRequest { Id = ValidArticleId, Article = EditArticleInputFactory.Create() };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditArticleCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditArticleCommand>(), ct))
            .ReturnsAsync(ArticleErrors.TournamentNotFound(new Neba.Api.Features.Tournaments.Domain.TournamentId("01ARZ3NDEKTSV4RRFFQ69G5FAV")));

        var endpoint = Factory.Create<EditArticleEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }

    [Fact(DisplayName = "HandleAsync should return 422 when the command returns validation errors")]
    public async Task HandleAsync_ShouldReturn422_WhenCommandReturnsValidationErrors()
    {
        // Arrange
        var request = new EditArticleRequest { Id = ValidArticleId, Article = EditArticleInputFactory.Create() };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditArticleCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditArticleCommand>(), ct))
            .ReturnsAsync(ArticleErrors.TitleRequired);

        var endpoint = Factory.Create<EditArticleEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "Configure should register a permission-protected PUT route under /news")]
    public void Configure_ShouldRegisterPermissionProtectedPutRoute_UnderNewsPath()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditArticleCommand, Updated>>(MockBehavior.Strict);
        var endpoint = Factory.Create<EditArticleEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("PUT");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("news"), "should be under the /news path");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}