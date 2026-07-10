using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.News.CreateArticle;
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Tournaments.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.News.CreateArticle;

internal sealed class CreateArticleEndpoint(Messaging.ICommandHandler<CreateArticleCommand, CreatedArticle> commandHandler)
    : Endpoint<CreateArticleRequest, ArticleResponse>
{
    private readonly Messaging.ICommandHandler<CreateArticleCommand, CreatedArticle> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post(string.Empty);
        Group<NewsEndpointGroup>();

        Options(options => options
            .WithVersionSet("News")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CreateArticle.PolicyName);

        Description(description => description
            .WithName("CreateArticle")
            .WithTags("Admin")
            .Produces<ArticleResponse>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(CreateArticleRequest req, CancellationToken ct)
    {
        var command = new CreateArticleCommand
        {
            Title = req.Article.Title,
            Slug = req.Article.Slug,
            Content = req.Article.Content,
            PublicationStatus = PublicationStatus.FromName(req.Article.PublicationStatus),
            PublishDateUtc = req.Article.PublishDateUtc,
            TournamentId = string.IsNullOrWhiteSpace(req.Article.TournamentId)
                ? null
                : new TournamentId(req.Article.TournamentId)
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
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

        var response = new ArticleResponse
        {
            ArticleId = result.Value.Id.Value.ToString(),
            Slug = result.Value.Slug
        };

        // Stryker disable once Statement
        await Send.CreatedAtAsync(
            "GetArticle",
            routeValues: new { slug = result.Value.Slug },
            responseBody: response,
            cancellation: ct);
    }
}