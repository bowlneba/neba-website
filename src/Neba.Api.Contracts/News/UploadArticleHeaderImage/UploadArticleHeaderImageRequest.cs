using Microsoft.AspNetCore.Http;

namespace Neba.Api.Contracts.News.UploadArticleHeaderImage;

/// <summary>
/// Request model for uploading an article header image.
/// </summary>
public sealed record UploadArticleHeaderImageRequest
{
    /// <summary>
    /// The image file to be uploaded as the article header.
    /// </summary>
    public required IFormFile File { get; init; }
}