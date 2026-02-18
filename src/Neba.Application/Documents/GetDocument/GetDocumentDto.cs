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
    /// The UTC timestamp when the source document was last modified, as reported by the document management system, or null if not available.
    /// </summary>
    public DateTimeOffset? LastUpdated { get; init; }
}