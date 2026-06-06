using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.News.GetArticle;

namespace Neba.Api.Features.News.GetArticle;

internal sealed class GetArticleEndpointSummary
    : Summary<GetArticleEndpoint, GetArticleRequest>
{
    public GetArticleEndpointSummary()
    {
        Summary = "Gets a published article by slug.";
        Description = "Retrieves the full content of a news article by its URL-friendly slug. Returns 404 if the article does not exist, is in draft status, or has a publish date in the future.";

#pragma warning disable S1075 // URIs should not be hardcoded
        Response(200, "The article detail.",
            contentType: MediaTypeNames.Application.Json,
            example: new ArticleDetailResponse
            {
                Slug = "spring-2026-results",
                Title = "Spring 2026 Tournament Results",
                Content = "<p>The Spring 2026 season wrapped up with an exciting finals...</p>",
                HeaderImageUrl = new Uri("https://files.bowlneba.com/news/spring-2026/header.jpg", UriKind.Absolute),
                PublishDateUtc = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero),
                TournamentId = null,
                Attachments =
                [
                    new ArticleAttachmentResponse
                    {
                        DisplayName = "Results Sheet",
                        ContentType = "application/pdf",
                        Url = new Uri("https://files.bowlneba.com/news/spring-2026/results.pdf", UriKind.Absolute),
                    },
                ],
            });

        Response<Microsoft.AspNetCore.Http.HttpValidationProblemDetails>(404, "No article exists with the given slug.");
#pragma warning restore S1075 // URIs should not be hardcoded
    }
}