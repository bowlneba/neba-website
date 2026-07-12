using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.News.EditArticle;

internal sealed class EditArticleEndpoint(Messaging.ICommandHandler<EditArticleCommand, Updated> commandHandler)
    : Endpoint<EditArticleRequest>
{
    private readonly Messaging.ICommandHandler<EditArticleCommand, Updated> _commandHandler = commandHandler;

    public override void Configure()
    {
        Put("{id}");
        Group<NewsEndpointGroup>();

        Options(options => options
            .WithVersionSet("News")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditArticle.PolicyName);

        Description(description => description
            .WithName("EditArticle")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(EditArticleRequest req, CancellationToken ct)
    {
        var command = new EditArticleCommand
        {
            ArticleId = new ArticleId(req.Id),
            Title = req.Article.Title,
            Content = req.Article.Content,
            PublicationStatus = PublicationStatus.FromName(req.Article.PublicationStatus),
            PublishDate = req.Article.PublishDate,
            TournamentId = string.IsNullOrWhiteSpace(req.Article.TournamentId)
                ? null
                : new TournamentId(req.Article.TournamentId),
            HeaderImage = req.Article.HeaderImage is null
                ? null
                : new StoredFile
                {
                    Container = req.Article.HeaderImage.Container,
                    Path = req.Article.HeaderImage.Path,
                    ContentType = req.Article.HeaderImage.ContentType,
                    SizeInBytes = req.Article.HeaderImage.SizeInBytes
                },
            Attachments = [.. req.Article.Attachments.Select(attachment => new EditArticleAttachment
            {
                DisplayName = attachment.DisplayName,
                IsInline = attachment.IsInline,
                File = new StoredFile
                {
                    Container = attachment.Container,
                    Path = attachment.Path,
                    ContentType = attachment.ContentType,
                    SizeInBytes = attachment.SizeInBytes
                }
            })]
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                await Send.NotFoundAsync(ct);

                // Stryker disable once Statement
                return;
            }

            if (result.FirstError.Type == ErrorType.Conflict)
            {
                AddError(result.FirstError.Description);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);

                // Stryker disable once Statement
                return;
            }

            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }

            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);

            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}