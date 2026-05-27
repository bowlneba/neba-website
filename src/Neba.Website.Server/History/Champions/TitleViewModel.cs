namespace Neba.Website.Server.History.Champions;

/// <summary>View model representing a single tournament title in a bowler's career history modal.</summary>
public sealed record TitleViewModel
{
    /// <summary>The unique identifier of the tournament.</summary>
    public required string TournamentId { get; init; }

    /// <summary>The display name of the tournament.</summary>
    public required string TournamentName { get; init; }

    /// <summary>Formatted display date (e.g. "Apr 2024").</summary>
    public required string TournamentDate { get; init; }

    /// <summary>The tournament type label (e.g. "Singles", "Doubles").</summary>
    public required string TournamentType { get; init; }
}