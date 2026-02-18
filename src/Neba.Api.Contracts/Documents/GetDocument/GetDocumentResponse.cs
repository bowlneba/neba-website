namespace Neba.Api.Contracts.Documents.GetDocument;

/// <summary>
/// Represents the response for a GetDocument request.
/// </summary>
public sealed record GetDocumentResponse
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