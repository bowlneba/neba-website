namespace Neba.Application.Stats.GetSeasonStats;

/// <summary>
/// View model for the points race tournament statistics.
/// </summary>
public sealed record PointsRaceTournamentDto
{
    /// <summary>
    /// Gets the name of the tournament.
    /// </summary>
    public required string TournamentName { get; init; }

    /// <summary>
    /// Gets the date of the tournament.
    /// </summary>
    public required DateOnly TournamentDate { get; init; }

    /// <summary>
    /// Gets the cumulative points earned by the bowler in the tournament.
    /// </summary>
    public required int CumulativePoints { get; init; }
}