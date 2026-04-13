namespace Neba.Website.Server.Stats;

/// <summary>
/// View model for a points race series, containing the bowler's information and their results in the tournaments of the series.
/// </summary>
public sealed record PointsRaceSeriesViewModel
{
    /// <summary>
    /// The unique identifier of the bowler participating in the points race series.
    /// </summary>
    public required Ulid BowlerId { get; init; }

    /// <summary>
    /// The name of the bowler participating in the points race series.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// The collection of tournament results for the bowler in the points race series, where each item represents the performance of the bowler in a specific tournament, including points earned and placement.
    /// </summary>
    public required IReadOnlyCollection<PointsRaceTournamentViewModel> Results { get; init; }
}