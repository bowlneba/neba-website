namespace Neba.Api.Contracts.Tournaments.ListTournamentTypes;

/// <summary>
/// Represents a tournament type for display in pickers.
/// </summary>
public sealed record TournamentTypeSummaryResponse
{
    /// <summary>
    /// The name of the tournament type.
    /// </summary>
    public required string Name { get; init; }
}
