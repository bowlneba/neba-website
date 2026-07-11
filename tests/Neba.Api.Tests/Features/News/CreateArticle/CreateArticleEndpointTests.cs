using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.News;
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
        capturedCommand.PublishDate.ShouldBe(input.PublishDate);
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
        var input = ArticleInputFactory.Create(headerImage: headerImage, attachments: [attachment]);
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