using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.News.ListArticles;
using Neba.Api.Messaging;

namespace Neba.Api.Features.News.ListArticles;

internal sealed class ListArticlesEndpoint(IQueryHandler<ListArticlesQuery, PagedResult<ArticleSummaryDto>> queryHandler)
    : Endpoint<ListArticlesRequest, PaginationResponse<ArticleSummaryResponse>>
{
    private readonly IQueryHandler<ListArticlesQuery, PagedResult<ArticleSummaryDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get(string.Empty);
        Group<NewsEndpointGroup>();

        Options(options => options
            .WithVersionSet("News")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListArticles")
            .WithTags("Public")
            .Produces<PaginationResponse<ArticleSummaryResponse>>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest));
    }

    public override async Task HandleAsync(ListArticlesRequest req, CancellationToken ct)
    {
        var query = new ListArticlesQuery { Page = req.Page, PageSize = req.PageSize };
        var result = await _queryHandler.HandleAsync(query, ct);

        var response = new PaginationResponse<ArticleSummaryResponse>
        {
            Items = [.. result.Items.Select(a => new ArticleSummaryResponse
            {
                Slug = a.Slug,
                Title = a.Title,
                Excerpt = a.Excerpt,
                HeaderImageUrl = a.HeaderImageUrl,
                PublishDateUtc = a.PublishDateUtc,
            })],
            TotalItems = result.TotalItems,
            PageNumber = req.Page,
            PageSize = req.PageSize,
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}
