namespace Neba.Api.Contracts.Stats.GetSeasonStats;

/// <summary>
/// Request contract for retrieving season statistics, including various leaderboards and a season summary. The Year parameter is optional; if not provided, the current season will be used. Validation ensures that if Year is specified, it must be between 2000 and the current year.
/// </summary>
public sealed record GetSeasonStatsRequest
{
    /// <summary>
    /// The year for which to retrieve season statistics. Optional; if not provided, the current season will be used. Must be between 2019 and the current year if specified.
    /// </summary>
    public int? Year { get; init; }
}