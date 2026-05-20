using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.BowlingCenters.ListBowlingCenters;

internal sealed record ListBowlingCentersQuery
    : ICachedQuery<IReadOnlyCollection<BowlingCenterSummaryDto>>
{
    public CacheDescriptor Cache
        => CacheDescriptors.BowlingCenters.List;

    public TimeSpan Expiry
        => TimeSpan.FromDays(7);
}