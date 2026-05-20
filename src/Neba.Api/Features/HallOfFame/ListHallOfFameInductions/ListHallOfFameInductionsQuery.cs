using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.HallOfFame.ListHallOfFameInductions;

internal sealed record ListHallOfFameInductionsQuery
    : ICachedQuery<IReadOnlyCollection<HallOfFameInductionDto>>
{
    public CacheDescriptor Cache
        => CacheDescriptors.HallOfFame.ListInductions;

    public TimeSpan Expiry
        => TimeSpan.FromDays(100);
}