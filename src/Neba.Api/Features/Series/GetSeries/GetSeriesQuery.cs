using ErrorOr;

using Neba.Api.Caching;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.GetSeries;

/// <summary>Gets one Series. Cached; every Series command clears it.</summary>
internal sealed record GetSeriesQuery
    : ICachedQuery<ErrorOr<SeriesDetailDto>>
{
    public required SeriesId Id { get; init; }

    public CacheDescriptor Cache
        => CacheDescriptors.Series.Detail(Id);

    public TimeSpan Expiry
        => TimeSpan.FromHours(12);
}
