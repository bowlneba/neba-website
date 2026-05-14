using ErrorOr;

using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Documents.GetDocument;

internal sealed record GetDocumentQuery
    : ICachedQuery<ErrorOr<GetDocumentDto>>
{
    public required string DocumentName { get; init; }

    public CacheDescriptor Cache
        => CacheDescriptors.Documents.Content(DocumentName);

    public TimeSpan Expiry
        => TimeSpan.FromDays(7);
}