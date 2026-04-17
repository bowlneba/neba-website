using Microsoft.Extensions.Logging;

namespace Neba.Website.Server.Stats;

internal static partial class SeasonStatsLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "[SeasonStats] Failed to load season stats.")]
    public static partial void LogFailedToLoadSeasonStats(
        this ILogger<SeasonStats> logger,
        Exception ex);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "[SeasonStats] Failed to load stats for year {Year}.")]
    public static partial void LogFailedToLoadSeasonStatsForYear(
        this ILogger<SeasonStats> logger,
        Exception ex,
        int year);
}
