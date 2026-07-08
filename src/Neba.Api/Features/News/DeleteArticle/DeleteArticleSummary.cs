using FastEndpoints;

namespace Neba.Api.Features.News.DeleteArticle;

internal sealed class DeleteArticleSummary : Summary<DeleteArticleEndpoint>
{
    public DeleteArticleSummary()
    {
        Summary = "Deletes a news article.";
        Description = "Permanently deletes the article and its attachments. Returns 204 whether or not an article with the given ID existed, so callers cannot use this endpoint to probe for article existence. Requires the News.DeleteArticle permission.";

        Response(204, "Article deleted, or no article existed with the given ID.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the News.DeleteArticle permission.");
    }
}
