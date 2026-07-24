using Neba.Api.Contracts.Tournaments.CreateTournament;

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
    /// The tournament fields to update. Every field on a tournament is editable, so this reuses
    /// <see cref="TournamentInput"/> rather than declaring a separate, identical shape.
    /// </summary>
    public required TournamentInput Tournament { get; init; }
}