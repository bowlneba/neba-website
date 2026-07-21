namespace Neba.Api.Contracts.Tournaments.CreateTournament;

/// <summary>
/// Response returned after successfully creating a tournament.
/// </summary>
public sealed record CreatedTournamentResponse
{
    /// <summary>
    /// The ULID string that uniquely identifies the newly created tournament.
    /// </summary>
    public required string TournamentId { get; init; }
}
