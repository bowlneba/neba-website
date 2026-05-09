using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;
using Neba.Domain.Tournaments;

namespace Neba.Application.Stats.BoyProgression;

/// <summary>One row per <c>HistoricalTournamentResult</c>. Used exclusively to compute BOY point progressions.</summary>
public sealed record BoyProgressionResultDto
{
    /// <summary>The bowler's unique identifier.</summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required Name BowlerName { get; init; }

    /// <summary>The tournament's unique identifier.</summary>
    public required TournamentId TournamentId { get; init; }

    /// <summary>The tournament's display name.</summary>
    public required string TournamentName { get; init; }

    /// <summary>The tournament's start date. Used for ordering results chronologically.</summary>
    public required DateOnly TournamentDate { get; init; }

    /// <summary>Whether the tournament counts toward the Open BOY race.</summary>
    public required bool StatsEligible { get; init; }

    /// <summary>The tournament type, used for specialty-race eligibility in Phase 2.</summary>
    public required TournamentType TournamentType { get; init; }

    /// <summary>The raw points listed on this result row.</summary>
    public required int Points { get; init; }

    /// <summary>The side cut identifier, or <c>null</c> if this is a main-cut result.</summary>
    public required int? SideCutId { get; init; }
}
