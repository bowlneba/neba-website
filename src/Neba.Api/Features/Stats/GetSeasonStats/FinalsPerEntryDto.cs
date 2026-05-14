using Neba.Domain.Bowlers;

namespace Neba.Api.Features.Stats.GetSeasonStats;

/// <summary>
/// A leaderboard entry for the Finals per Entry leaderboard, representing a bowler's Finals
/// appearance rate relative to their number of tournament entries. Ordered descending by ratio.
/// </summary>
public sealed record FinalsPerEntryDto
{
    /// <summary>The unique identifier of the bowler.</summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required Name BowlerName { get; init; }

    /// <summary>The number of Finals appearances during the season.</summary>
    public required int Finals { get; init; }

    /// <summary>The total number of tournament entries during the season.</summary>
    public required int Entries { get; init; }

    /// <summary>Pre-computed ratio of Finals appearances to total entries.</summary>
    public required decimal FinalsPerEntry { get; init; }
}