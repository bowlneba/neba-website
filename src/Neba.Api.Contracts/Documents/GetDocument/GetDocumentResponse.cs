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
}