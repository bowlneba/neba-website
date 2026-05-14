using Neba.Domain.Bowlers;

namespace Neba.Api.Features.Stats.GetSeasonStats;

/// <summary>
/// A leaderboard entry for the Match Play Appearances leaderboard, representing the number of Finals
/// appearances for a bowler during the season. Ordered descending by Finals count. Bowlers with zero
/// Finals are excluded.
/// </summary>
public sealed record MatchPlayAppearancesDto
{
    /// <summary>The unique identifier of the bowler.</summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required Name BowlerName { get; init; }

    /// <summary>The number of Finals appearances during the season.</summary>
    public required int Finals { get; init; }

    /// <summary>The number of distinct tournaments the bowler participated in during the season.</summary>
    public required int Tournaments { get; init; }

    /// <summary>The total number of tournament entries during the season.</summary>
    public required int Entries { get; init; }
}