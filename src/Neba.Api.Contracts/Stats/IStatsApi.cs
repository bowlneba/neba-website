using Neba.Api.Contracts.Stats.GetSeasonStats;

using Refit;

namespace Neba.Api.Contracts.Stats;

/// <summary>
/// Defines the stats API contract.
/// </summary>
public interface IStatsApi
{
    /// <summary>
    /// Retrieves aggregated statistics for a specified season, including award standings and various leaderboards.
    /// </summary>
    /// <param name="year">
    /// The year for which to retrieve statistics.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// A response containing the season statistics.
    /// </returns>
    [Get("/stats")]
    Task<IApiResponse<GetSeasonStatsResponse>> GetSeasonStatsAsync([Query] int? year, CancellationToken cancellationToken = default);
}