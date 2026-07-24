namespace Neba.Api.Contracts.Tournaments.EditTournament;

/// <summary>
/// Edits an existing tournament.
/// </summary>
public sealed record EditTournamentRequest
{
    /// <summary>
    /// The ULID string identifying the tournament to edit.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The tournament fields to update.
    /// </summary>
    public required EditTournamentInput Tournament { get; init; }
}