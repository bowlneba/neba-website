using FastEndpoints;

using Neba.Api.Contracts.Uploads;

namespace Neba.Api.Features.News.UploadArticleHeaderImage;

internal sealed class UploadArticleHeaderImageSummary : Summary<UploadArticleHeaderImageEndpoint>
{
    public UploadArticleHeaderImageSummary()
    {
        Summary = "Uploads a news article header image.";
        Description = "Stages an image file in blob storage ahead of article creation and returns a pointer to it. The pointer is orphaned (and later swept) unless it's included as HeaderImage in a subsequent CreateArticle command. Requires the News.CreateArticle permission.";

        Response<UploadedFileResponse>(200, "File uploaded.");
        Response(400, "File missing, wrong content type, or too large.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the News.CreateArticle permission.");
    }
}