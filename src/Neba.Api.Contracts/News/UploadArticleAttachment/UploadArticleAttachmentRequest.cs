using Microsoft.AspNetCore.Http;

namespace Neba.Api.Contracts.News.UploadArticleAttachment;

/// <summary>
/// Request model for uploading an article attachment.
/// </summary>
public sealed record UploadArticleAttachmentRequest
{
    /// <summary>
    /// The file to be uploaded as an article attachment.
    /// </summary>
    public required IFormFile File { get; init; }
}