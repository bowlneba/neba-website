using ErrorOr;

using Neba.Application.Caching;
using Neba.Application.Messaging;

namespace Neba.Application.Documents.GetDocument;

/// <summary>
/// A query to retrieve a document by its name.
/// </summary>
public sealed record GetDocumentQuery
    : ICachedQuery<ErrorOr<GetDocumentDto>>
{
    /// <summary>
    /// The name of the document to retrieve.
    /// </summary>
    public required string DocumentName { get; init; }

    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.Documents.Content(DocumentName);

    /// <inheritdoc />
    public TimeSpan Expiry
        => TimeSpan.FromDays(7);
}