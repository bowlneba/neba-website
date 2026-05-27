namespace Neba.Website.Server.History.Champions;

/// <summary>View model representing a single title won by a bowler, used in the year-by-year champions table.</summary>
public sealed record BowlerTitleViewModel
{
    /// <summary>The bowler's unique identifier.</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>The unique identifier of the tournament in which the title was won.</summary>
    public required string TournamentId { get; init; }

    /// <summary>The calendar month (1–12) of the tournament.</summary>
    public required int TournamentMonth { get; init; }

    /// <summary>The calendar year of the tournament.</summary>
    public required int TournamentYear { get; init; }

    /// <summary>The tournament type label (e.g. "Singles", "Doubles").</summary>
    public required string TournamentType { get; init; }

    /// <summary>Whether the bowler has been inducted into the Hall of Fame.</summary>
    public required bool HallOfFame { get; init; }
}