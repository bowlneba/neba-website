using Neba.Domain.Bowlers;

namespace Neba.Application.Stats.GetSeasonStats;

/// <summary>
/// A leaderboard entry for the Points per Tournament leaderboard, representing a bowler's average
/// points earned per tournament participated in during the season. Ordered descending by ratio.
/// The ratio is pre-computed.
/// </summary>
public sealed record PointsPerTournamentDto
{
    /// <summary>The unique identifier of the bowler.</summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required Name BowlerName { get; init; }

    /// <summary>Total Bowler of the Year points accumulated during the season.</summary>
    public required int Points { get; init; }

    /// <summary>The number of distinct tournaments the bowler participated in during the season.</summary>
    public required int Tournaments { get; init; }

    /// <summary>Pre-computed ratio of points to tournaments participated in.</summary>
    public required decimal PointsPerTournament { get; init; }
}
