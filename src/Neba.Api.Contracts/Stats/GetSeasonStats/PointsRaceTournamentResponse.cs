namespace Neba.Api.Contracts.Stats.GetSeasonStats;

/// <summary>A single data point in a bowler's Bowler of the Year points race series, representing the cumulative points after one tournament.</summary>
public sealed record PointsRaceTournamentResponse
{
    /// <summary>The name of the tournament.</summary>
    public required string TournamentName { get; init; }

    /// <summary>The date on which the tournament took place.</summary>
    public required DateOnly TournamentDate { get; init; }

    /// <summary>Cumulative Bowler of the Year points earned by the bowler after this tournament.</summary>
    public required int CumulativePoints { get; init; }
}
