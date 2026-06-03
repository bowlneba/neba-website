namespace Neba.Api.Features.News.GetArticle;

/// <summary>
/// Represents an attachment of a news article, such as a file or image.
/// </summary>
public sealed record ArticleAttachmentDto
{
    /// <summary>
    /// The display name of the attachment, which may differ from the actual file name.
    /// </summary>
    public required string DisplayName { get; init; }
    
    /// <summary>
    /// The name of the storage container where the attachment is stored. This is used to locate the attachment in the storage system.
    /// </summary>
    public required string Container { get; init; }

    /// <summary>
    /// The path to the attachment within the storage container. This is used in conjunction with the container name to access the attachment.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// The URL to access the attachment. This may be null if the attachment is not accessible via a URL or if the URL is not provided.
    /// </summary>
    public Uri? Url { get; init; }
}