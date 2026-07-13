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

    /// <summary>Whether the attachment is embedded inline in the article body rather than listed as a downloadable file.</summary>
    public required bool IsInline { get; init; }

    /// <summary>
    /// The Azure Blob Storage container the attachment is stored in, populated only when the caller has
    /// article management permission (needed to resubmit the attachment unchanged on edit). Null for
    /// anonymous callers.
    /// </summary>
    public string? Container { get; init; }

    /// <summary>
    /// The blob path of the attachment, populated only when the caller has article management permission.
    /// Null for anonymous callers.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// The attachment's file size in bytes, populated only when the caller has article management
    /// permission. Null for anonymous callers.
    /// </summary>
    public long? SizeInBytes { get; init; }
}