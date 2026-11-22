using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.ListSeries;

/// <summary>
/// Lists every Series, newest season first. Cached; every Series command clears it. The expiry is short
/// because each Series' next Stage depends on today's date.
/// </summary>
internal sealed record ListSeriesQuery
    : ICachedQuery<IReadOnlyCollection<SeriesSummaryDto>>
{
    public CacheDescriptor Cache
        => CacheDescriptors.Series.List;

    public TimeSpan Expiry
        => TimeSpan.FromHours(1);
}
