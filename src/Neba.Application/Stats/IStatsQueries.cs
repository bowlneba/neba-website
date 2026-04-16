using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Seasons;

namespace Neba.Application.Stats;

/// <summary>
/// Defines queries for retrieving statistical data about bowlers and seasons. This interface abstracts the data retrieval
/// logic for performance metrics, classifications, and award standings, allowing for flexible implementations that can
/// source data from various repositories or services.
/// </summary>
public interface IStatsQueries
{
    /// <summary>
    /// Retrieves comprehensive season statistics for all bowlers who participated in the specified Season. The returned data includes
    /// performance metrics, classification flags (e.g., Rookie, Senior), award points, and financial totals for each bowler. This method is designed to support season-end reporting and award determinations
    /// </summary>
    /// <param name="seasonId">The unique identifier of the season for which to retrieve statistics.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only collection of bowler season statistics.</returns>
    Task<IReadOnlyCollection<BowlerSeasonStatsDto>> GetBowlerSeasonStatsAsync(SeasonId seasonId, CancellationToken cancellationToken);
}