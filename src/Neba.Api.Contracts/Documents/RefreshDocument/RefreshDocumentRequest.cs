namespace Neba.Api.Contracts.Documents.RefreshDocument;

/// <summary>
/// Represents the request for a RefreshDocument operation.
/// </summary>
public sealed record RefreshDocumentRequest
{
    /// <summary>
    /// The name of the document whose cached copy should be cleared.
    /// </summary>
    public required string DocumentName { get; init; }
}
