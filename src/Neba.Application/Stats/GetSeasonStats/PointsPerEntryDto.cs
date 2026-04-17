using Neba.Domain.Bowlers;

namespace Neba.Application.Stats.GetSeasonStats;

/// <summary>
/// A leaderboard entry for the Points per Entry leaderboard, representing a bowler's average points
/// earned per tournament entry during the season. Ordered descending by ratio. Bowlers with zero
/// entries or zero points are excluded. The ratio is pre-computed.
/// </summary>
public sealed record PointsPerEntryDto
{
    /// <summary>The unique identifier of the bowler.</summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required Name BowlerName { get; init; }

    /// <summary>Pre-computed ratio of points to total entries.</summary>
    public required decimal PointsPerEntry { get; init; }

    /// <summary>Total Bowler of the Year points accumulated during the season.</summary>
    public required int Points { get; init; }

    /// <summary>Total number of tournament entries during the season.</summary>
    public required int Entries { get; init; }
}
