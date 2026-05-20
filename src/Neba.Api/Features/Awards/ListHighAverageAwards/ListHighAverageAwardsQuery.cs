using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Awards.ListHighAverageAwards;

internal sealed record ListHighAverageAwardsQuery
    : ICachedQuery<IReadOnlyCollection<HighAverageAwardDto>>
{
    public CacheDescriptor Cache
        => CacheDescriptors.Awards.ListHighAverageAwards;

    public TimeSpan Expiry
        => new(365, 0, 0, 0); // 1 year
}