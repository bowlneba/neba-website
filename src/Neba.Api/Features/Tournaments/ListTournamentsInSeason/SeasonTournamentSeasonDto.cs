using Neba.Api.Features.Seasons.Domain;

namespace Neba.Api.Features.Tournaments.ListTournamentsInSeason;

/// <summary>
/// Season a listed tournament belongs to.
/// </summary>
public sealed record SeasonTournamentSeasonDto
{
    /// <summary>
    /// Unique season identifier.
    /// </summary>
    public required SeasonId Id { get; init; }

    /// <summary>
    /// Human-readable season description, such as "2025 Season".
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Date the season begins.
    /// </summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// Date the season ends.
    /// </summary>
    public required DateOnly EndDate { get; init; }
}
