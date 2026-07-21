namespace Neba.Api.Contracts.Tournaments.CreateTournament;

/// <summary>
/// Creates a tournament.
/// </summary>
public sealed record CreateTournamentRequest
{
    /// <summary>
    /// The tournament fields to create.
    /// </summary>
    public required TournamentInput Tournament { get; init; }
}
