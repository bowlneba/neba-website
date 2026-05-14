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

    /// <summary>The bowler's date of birth, used for age-gated race eligibility (Senior, SuperSenior, Youth).</summary>
    public required DateOnly? BowlerDateOfBirth { get; init; }

    /// <summary>The bowler's gender, used for Woman race eligibility.</summary>
    public required Gender? BowlerGender { get; init; }

    /// <summary>The tournament's unique identifier.</summary>
    public required TournamentId TournamentId { get; init; }

    /// <summary>The tournament's display name.</summary>
    public required string TournamentName { get; init; }

    /// <summary>The tournament's start date. Used for ordering results chronologically.</summary>
    public required DateOnly TournamentDate { get; init; }

    /// <summary>The tournament's end date. Age eligibility for Senior, SuperSenior, and Youth races is evaluated against this date.</summary>
    public required DateOnly TournamentEndDate { get; init; }

    /// <summary>Whether the tournament counts toward the Open BOY race.</summary>
    public required bool StatsEligible { get; init; }

    /// <summary>The tournament type, used for specialty-race eligibility.</summary>
    public required TournamentType TournamentType { get; init; }

    /// <summary>The raw points listed on this result row.</summary>
    public required int Points { get; init; }

    /// <summary>The side cut identifier, or <c>null</c> if this is a main-cut result.</summary>
    public required int? SideCutId { get; init; }

    /// <summary>The side cut's display name, used to derive which BOY race it targets. <c>null</c> for main-cut results.</summary>
    public required string? SideCutName { get; init; }
}