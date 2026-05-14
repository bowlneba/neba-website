namespace Neba.Api.Features.Tournaments.GetTournament;

/// <summary>
/// Represents a data transfer object containing basic information about a tournament bowling center, including its name
/// and location.
/// </summary>
public sealed record TournamentBowlingCenterDto
{
    /// <summary>
    /// Gets the name associated with this instance.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the name of the city associated with this instance.
    /// </summary>
    public required string City { get; init; }

    /// <summary>
    /// Gets the name of the state associated with this instance.
    /// </summary>
    public required string State { get; init; }
}
