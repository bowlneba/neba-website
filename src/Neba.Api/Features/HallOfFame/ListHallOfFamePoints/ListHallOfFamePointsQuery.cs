using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.HallOfFame.ListHallOfFamePoints;

internal sealed record ListHallOfFamePointsQuery
    : ICachedQuery<HallOfFamePointsDto>
{
    public CacheDescriptor Cache
        => CacheDescriptors.HallOfFame.ListPoints;

    public TimeSpan Expiry
        => TimeSpan.FromDays(14);
}