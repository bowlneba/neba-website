using ErrorOr;

using Neba.Api.Caching;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.HallOfFame.GetHallOfFamePointsDetail;

internal sealed record GetHallOfFamePointsDetailQuery
    : ICachedQuery<ErrorOr<HallOfFamePointsDetailDto>>
{
    public required BowlerId BowlerId { get; init; }

    public CacheDescriptor Cache
        => CacheDescriptors.HallOfFame.PointsDetail(BowlerId);

    public TimeSpan Expiry
        => TimeSpan.FromDays(14);
}