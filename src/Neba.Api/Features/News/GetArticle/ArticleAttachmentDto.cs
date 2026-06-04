namespace Neba.Api.Features.News.GetArticle;

/// <summary>
/// Represents an attachment of a news article, such as a file or image.
/// </summary>
public sealed record ArticleAttachmentDto
{
    /// <summary>The display name of the attachment, which may differ from the actual file name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>The MIME content type of the attachment (e.g., "image/jpeg", "application/pdf").</summary>
    public required string ContentType { get; init; }

    /// <summary>The URL to access the attachment.</summary>
    public required Uri Url { get; init; }
}