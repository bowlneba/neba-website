using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Awards.ListBowlerOfTheYearAwards;

internal sealed record ListBowlerOfTheYearAwardsQuery
    : ICachedQuery<IReadOnlyCollection<BowlerOfTheYearAwardDto>>
{
    public CacheDescriptor Cache
        => CacheDescriptors.Awards.ListBowlerOfTheYearAwards;

    public TimeSpan Expiry
        => new(365, 0, 0, 0); // 1 year
}