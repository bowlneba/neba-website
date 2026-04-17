using Neba.Application.Seasons;

namespace Neba.Application.Stats.GetSeasonStats;

/// <summary>
/// Data transfer object representing the season's statistics, including individual bowler statistics, the
/// </summary>
public sealed record SeasonStatsDto
{
    /// <summary>
    /// Gets the season for which the statistics are being retrieved, representing the specific time period and context for the performance metrics and achievements included in the season's statistics. This property provides essential context for understanding the data and allows for accurate interpretation of the season's performance and achievements.
    /// </summary>
    public required SeasonDto Season { get; init; }

    /// <summary>
    /// Gets a collection of seasons for which statistics are available, representing the range of time periods for which performance metrics and achievements have been recorded and can be retrieved. This collection provides insight into the historical context of the season's statistics and allows for comparison across different seasons, highlighting trends and changes in performance over time.
    /// </summary>
    public required IReadOnlyCollection<SeasonDto> SeasonsWithStats { get; init; }

    /// <summary>
    /// Gets a collection of individual bowler season statistics, representing the performance metrics and achievements of each bowler throughout the season. This collection provides detailed insights into the performance of each participant, allowing for analysis and comparison of their achievements during the season's events.
    /// </summary>
    public required IReadOnlyCollection<BowlerSeasonStatsDto> BowlerStats { get; init; }

    /// <summary>
    /// Gets a collection of series data for the Bowler of the Year points
    /// </summary>
    public required IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto> BowlerOfTheYearRace { get; init; }

    /// <summary>
    /// Gets a summary of the season's statistics, including total entries, total prize money, and other aggregated performance metrics. This summary provides a concise overview of the season's overall performance and key highlights, allowing for a quick assessment of the season's success and notable achievements.
    /// </summary>
    public required SeasonStatsSummaryDto Summary { get; init; }
}