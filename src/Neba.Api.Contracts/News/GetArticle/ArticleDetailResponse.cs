namespace Neba.Api.Contracts.News.GetArticle;

/// <summary>
/// Represents the full detail of a published news article.
/// </summary>
public sealed record ArticleDetailResponse
{
    /// <summary>
    /// The ULID string that uniquely identifies the article.
    /// </summary>
    public required string ArticleId { get; init; }

    /// <summary>
    /// The URL-friendly identifier for the article.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// The publication status of the article, which indicates whether the article is published, draft, or archived. This status is used to control the visibility and accessibility of the article in the application.
    /// </summary>
    public required string PublicationStatus { get; init; }

    /// <summary>
    /// The title of the article.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The full HTML content of the article.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// A public URL for the article's header image, or null if no image is set.
    /// </summary>
    public Uri? HeaderImageUrl { get; init; }

    /// <summary>
    /// The Azure Blob Storage container the header image is stored in. Populated only when the caller
    /// has article management permission (used to resubmit the header image unchanged when editing the
    /// article); null for anonymous callers or when there is no header image.
    /// </summary>
    public string? HeaderImageContainer { get; init; }

    /// <summary>
    /// The blob path of the header image. Populated only when the caller has article management
    /// permission; null for anonymous callers or when there is no header image.
    /// </summary>
    public string? HeaderImagePath { get; init; }

    /// <summary>
    /// The MIME content type of the header image. Populated only when the caller has article management
    /// permission; null for anonymous callers or when there is no header image.
    /// </summary>
    public string? HeaderImageContentType { get; init; }

    /// <summary>
    /// The header image's file size in bytes. Populated only when the caller has article management
    /// permission; null for anonymous callers or when there is no header image.
    /// </summary>
    public long? HeaderImageSizeInBytes { get; init; }

    /// <summary>
    /// The UTC date and time when the article was published.
    /// </summary>
    public required DateTimeOffset PublishDateUtc { get; init; }

    /// <summary>
    /// The ULID string of an associated tournament, or null if the article is not linked to a tournament.
    /// </summary>
    public string? TournamentId { get; init; }

    /// <summary>
    /// Files attached to the article.
    /// </summary>
    public IReadOnlyCollection<ArticleAttachmentResponse> Attachments { get; init; } = [];
}