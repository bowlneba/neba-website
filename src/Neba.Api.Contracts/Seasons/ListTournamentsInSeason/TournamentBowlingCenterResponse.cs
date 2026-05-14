namespace Neba.Api.Contracts.Seasons.ListTournamentsInSeason;

/// <summary>
/// Simplified bowling center details included in a tournament summary response.
/// </summary>
public sealed record TournamentBowlingCenterResponse
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