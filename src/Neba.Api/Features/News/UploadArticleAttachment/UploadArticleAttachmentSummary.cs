using FastEndpoints;

using Neba.Api.Contracts.Uploads;

namespace Neba.Api.Features.News.UploadArticleAttachment;

internal sealed class UploadArticleAttachmentSummary : Summary<UploadArticleAttachmentEndpoint>
{
    public UploadArticleAttachmentSummary()
    {
        Summary = "Uploads a news article attachment (or inline embedded image).";
        Description = "Stages a file in blob storage ahead of article creation and returns a pointer to it. Used both for regular downloadable attachments and for images embedded inline in the article body via the rich text editor — the distinction (IsInline) is supplied later, when the pointer is included in the CreateArticle command's Attachments list. The pointer is orphaned (and later swept) unless it's claimed by a subsequent CreateArticle command. Requires the News.CreateArticle permission.";

        Response<UploadedFileResponse>(200, "File uploaded.");
        Response(400, "File missing, wrong content type, or too large.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the News.CreateArticle permission.");
    }
}