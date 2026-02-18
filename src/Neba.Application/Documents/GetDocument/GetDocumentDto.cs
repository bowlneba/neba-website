namespace Neba.Application.Documents.GetDocument;

/// <summary>
/// The result of a get document query, containing the rendered HTML content and cache metadata.
/// </summary>
public sealed record GetDocumentDto
{
    /// <summary>
    /// The content of the document in HTML format.
    /// </summary>
    public required string Html { get; init; }

    /// <summary>
    /// The UTC timestamp when the document was last cached from the source, or null if not available.
    /// </summary>
    public DateTimeOffset? CachedAt { get; init; }
}
