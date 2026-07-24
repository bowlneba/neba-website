namespace Neba.Api.Features.Tournaments.ListTournamentTypes;

/// <summary>
/// Represents a summary of a tournament type, including its name.
/// </summary>
public sealed record TournamentTypeSummaryDto
{
    /// <summary>
    /// Gets the name associated with the current instance.
    /// </summary>
    public required string Name { get; init; }
}