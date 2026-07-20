using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.ReferenceData.ListUsStates;

internal sealed record ListUsStatesQuery
    : ICachedQuery<IReadOnlyCollection<UsStateDto>>
{
    public CacheDescriptor Cache => CacheDescriptors.ReferenceData.UsStates;

    public TimeSpan Expiry => TimeSpan.FromDays(30);
}
