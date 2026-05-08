namespace Neba.Api.Contracts.Tournaments.GetTournament;

/// <summary>
/// Season summary included in a tournament detail response.
/// </summary>
public sealed record TournamentDetailSeasonResponse
{
    /// <summary>
    /// The unique season identifier as a ULID string.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable season description (e.g., "2024-2025 Season").
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
