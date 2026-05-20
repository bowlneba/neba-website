using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Stats.GetSeasonStats;

/// <summary>
/// View model for a points race series, containing the bowler's information and their results in the tournaments of the series.
/// </summary>
public sealed record BowlerOfTheYearPointsRaceSeriesDto
{
    /// <summary>
    /// The unique identifier of the bowler participating in the points race series.
    /// </summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>
    /// The name of the bowler participating in the points race series.
    /// </summary>
    public required Name BowlerName { get; init; }

    /// <summary>
    /// The collection of tournament results for the bowler in the points race series, where each item represents the performance of the bowler in a specific tournament, including points earned and placement.
    /// </summary>
    public required IReadOnlyCollection<BowlerOfTheYearPointsRaceTournamentDto> Results { get; init; }
}