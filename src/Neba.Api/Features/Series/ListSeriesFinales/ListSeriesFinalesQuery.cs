using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.ListSeriesFinales;

/// <summary>Lists the Finale of every Series that has one. Cached; every Series command clears it.</summary>
internal sealed record ListSeriesFinalesQuery
    : ICachedQuery<IReadOnlyCollection<SeriesFinaleSummaryDto>>
{
    public CacheDescriptor Cache
        => CacheDescriptors.Series.ListFinales;

    public TimeSpan Expiry
        => TimeSpan.FromHours(12);
}
