namespace Neba.Api.Contracts.Documents.GetDocument;

/// <summary>
/// Represents the request for a GetDocument operation.
/// </summary>
public sealed record GetDocumentRequest
{
    /// <summary>
    /// The name of the document to retrieve.
    /// </summary>
    public required string DocumentName { get; init; }
}