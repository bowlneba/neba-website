using ErrorOr;

namespace Neba.Application.Stats;

internal static class StatsErrors
{
    public static readonly Error SeasonHasNoStats = Error.NotFound(
        code: "Stats.SeasonHasNoStats",
        description: "The requested season does not have any stats available.");
}