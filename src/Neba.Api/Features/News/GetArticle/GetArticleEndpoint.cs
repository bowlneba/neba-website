using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.News.GetArticle;
using Neba.Api.Messaging;

namespace Neba.Api.Features.News.GetArticle;

internal sealed class GetArticleEndpoint(
    IQueryHandler<GetArticleQuery, ErrorOr<ArticleDetailDto>> queryHandler)
    : Endpoint<GetArticleRequest, ArticleDetailResponse>
{
    private readonly IQueryHandler<GetArticleQuery, ErrorOr<ArticleDetailDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("{slug}");
        Group<NewsEndpointGroup>();

        Options(options => options
            .WithVersionSet("News")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("GetArticle")
            .WithTags("Public")
            .Produces<ArticleDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
    }

    public override async Task HandleAsync(GetArticleRequest req, CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new GetArticleQuery { Slug = req.Slug }, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                await Send.NotFoundAsync(ct);
                // Stryker disable once Statement
                return;
            }

            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }

            await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct);
            // Stryker disable once Statement
            return;
        }

        var dto = result.Value;

        var response = new ArticleDetailResponse
        {
            Slug = dto.Slug,
            Title = dto.Title,
            Content = dto.Content,
            HeaderImageUrl = dto.HeaderImageUrl,
            PublishDateUtc = dto.PublishDateUtc,
            TournamentId = dto.TournamentId?.Value.ToString(),
            Attachments = [.. dto.Attachments.Select(a => new ArticleAttachmentResponse
            {
                DisplayName = a.DisplayName,
                Url = a.Url,
            })],
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}