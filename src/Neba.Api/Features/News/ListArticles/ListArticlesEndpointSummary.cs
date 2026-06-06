using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.News.ListArticles;

namespace Neba.Api.Features.News.ListArticles;

internal sealed class ListArticlesEndpointSummary
    : Summary<ListArticlesEndpoint>
{
    public ListArticlesEndpointSummary()
    {
        Summary = "Lists published articles.";
        Description = "Retrieves a paginated list of published articles ordered by publish date descending. Only articles with a publish date on or before now are included.";

#pragma warning disable S1075 // URIs should not be hardcoded
        Response(200, "The paginated list of articles.",
            contentType: MediaTypeNames.Application.Json,
            example: new PaginationResponse<ArticleSummaryResponse>
            {
                Items =
                [
                    new ArticleSummaryResponse
                    {
                        Slug = "spring-2026-results",
                        Title = "Spring 2026 Tournament Results",
                        Excerpt = "The Spring 2026 season wrapped up with an exciting finals...",
                        HeaderImageUrl = new Uri("https://files.bowlneba.com/news/spring-2026/header.jpg", UriKind.Absolute),
                        PublishDateUtc = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero),
                    },
                ],
                TotalItems = 42,
                PageNumber = 1,
                PageSize = 10,
            });

        Response<Microsoft.AspNetCore.Http.HttpValidationProblemDetails>(400, "The page or pageSize parameter is invalid.");
#pragma warning restore S1075 // URIs should not be hardcoded

    }
}