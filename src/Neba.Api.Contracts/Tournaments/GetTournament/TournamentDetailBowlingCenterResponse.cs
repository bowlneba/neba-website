namespace Neba.Api.Contracts.Tournaments.GetTournament;

/// <summary>
/// Bowling center summary included in a tournament detail response.
/// </summary>
public sealed record TournamentDetailBowlingCenterResponse
{
    /// <summary>
    /// The bowling center's public-facing name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// City where the bowling center is located.
    /// </summary>
    public required string City { get; init; }

    /// <summary>
    /// State where the bowling center is located (two-letter code, e.g., "MA").
    /// </summary>
    public required string State { get; init; }
}
