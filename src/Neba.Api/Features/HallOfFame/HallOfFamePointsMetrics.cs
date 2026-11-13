using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Neba.Api.Features.HallOfFame;

/// <summary>
/// Counts Hall of Fame points loads. A handler records one after it loads data without error; the cache calls it on a miss, and may also call it on a background refresh.
/// </summary>
internal static class HallOfFamePointsMetrics
{
    private const string QueryTagName = "query";

    internal const string MeterName = "Neba.HallOfFame";
    internal const string CacheMissCountName = "neba.hall_of_fame.points.cache_miss.count";

    internal const string ListQuery = "list";
    internal const string DetailQuery = "detail";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> CacheMissCount
        = Meter.CreateCounter<long>(
            CacheMissCountName,
            description: "Counts Hall of Fame points cache misses by query (list, detail).");

    public static void RecordCacheMiss(string query)
        => CacheMissCount.Add(1, new TagList { { QueryTagName, query } });
}