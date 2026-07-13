namespace Neba.Api.Contracts.News.GetArticle;

/// <summary>
/// Represents a file attachment on a news article.
/// </summary>
public sealed record ArticleAttachmentResponse
{
    /// <summary>
    /// The display name for the attachment link.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// The MIME content type of the attachment (e.g., "image/jpeg", "application/pdf").
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// A public URL to view the attachment, or null if unavailable.
    /// </summary>
    public Uri? Url { get; init; }

    /// <summary>
    /// Whether the attachment is embedded inline in the article body rather than listed as a downloadable file.
    /// </summary>
    public required bool IsInline { get; init; }

    /// <summary>
    /// The Azure Blob Storage container the attachment is stored in. Populated only when the caller has
    /// article management permission (used to resubmit the attachment unchanged when editing the
    /// article); null for anonymous callers.
    /// </summary>
    public string? Container { get; init; }

    /// <summary>
    /// The blob path of the attachment. Populated only when the caller has article management
    /// permission; null for anonymous callers.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// The attachment's file size in bytes. Populated only when the caller has article management
    /// permission; null for anonymous callers.
    /// </summary>
    public long? SizeInBytes { get; init; }
}