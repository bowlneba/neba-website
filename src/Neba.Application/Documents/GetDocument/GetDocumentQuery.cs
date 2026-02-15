using Neba.Application.Messaging;

namespace Neba.Application.Documents.GetDocument;

/// <summary>
/// A query to retrieve a document by its name.
/// </summary>
public sealed record GetDocumentQuery
    : IQuery<string>
{
    /// <summary>
    /// The name of the document to retrieve.
    /// </summary>
    public required string DocumentName { get; init; }
}