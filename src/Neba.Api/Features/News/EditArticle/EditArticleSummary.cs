using FastEndpoints;

namespace Neba.Api.Features.News.EditArticle;

internal sealed class EditArticleSummary : Summary<EditArticleEndpoint>
{
    public EditArticleSummary()
    {
        Summary = "Edits a news article.";
        Description = "Replaces the article's editable fields (title, content, publication status, publish date, tournament link, header image, attachments). The slug is immutable and is not part of this request. Attachments are a full replace-set: any existing attachment missing from the request is removed. Requires the News.EditArticle permission.";

        Response(204, "Article updated.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the News.EditArticle permission.");
        Response(404, "No article exists with the given ID.");
        Response(409, "TournamentId does not reference an existing tournament.");
        Response(422, "Title or content failed a domain validation rule.");
    }
}
