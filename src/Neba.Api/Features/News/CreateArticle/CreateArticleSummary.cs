using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.News.CreateArticle;

namespace Neba.Api.Features.News.CreateArticle;

internal sealed class CreateArticleSummary : Summary<CreateArticleEndpoint>
{
    public CreateArticleSummary()
    {
        Summary = "Creates a news article.";
        Description = "Creates a draft or published article. Slug is derived from the title unless a staff-supplied override is given; either way it is normalized and must be unique. Requires the News.CreateArticle permission.";

#pragma warning disable S1075 // URIs should not be hardcoded
        Response(201, "Article created.",
            contentType: MediaTypeNames.Application.Json,
            example: new ArticleResponse
            {
                ArticleId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E",
                Slug = "spring-2026-results"
            });
#pragma warning restore S1075 // URIs should not be hardcoded

        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the News.CreateArticle permission.");
        Response(409, "Slug already taken, or TournamentId does not reference an existing tournament.");
        Response(422, "Title, content, or slug failed a domain validation rule.");
    }
}