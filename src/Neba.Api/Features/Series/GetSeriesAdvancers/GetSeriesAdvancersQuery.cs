using ErrorOr;

using Neba.Api.Caching;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.GetSeriesAdvancers;

/// <summary>Gets who advanced out of each Official Stage. Cached; a Stage status change clears it, and names and results expire after 5 minutes.</summary>
internal sealed record GetSeriesAdvancersQuery
    : ICachedQuery<ErrorOr<SeriesAdvancersDto>>
{
    public required SeriesId SeriesId { get; init; }

    public CacheDescriptor Cache
        => CacheDescriptors.Series.Advancers(SeriesId);

    public TimeSpan Expiry
        => TimeSpan.FromMinutes(5);
}
