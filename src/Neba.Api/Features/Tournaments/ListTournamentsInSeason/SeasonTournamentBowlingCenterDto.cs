namespace Neba.Api.Features.Tournaments.ListTournamentsInSeason;

/// <summary>
/// Represents a data transfer object containing information about a tournament bowling center.
/// </summary>
public sealed record SeasonTournamentBowlingCenterDto
{
    /// <summary>
    /// Gets the name associated with this instance.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the city where the bowling center is located.
    /// </summary>
    public required string City { get; init; }

    /// <summary>
    /// Gets the state associated with the current instance.
    /// </summary>
    public required string State { get; init; }
}
