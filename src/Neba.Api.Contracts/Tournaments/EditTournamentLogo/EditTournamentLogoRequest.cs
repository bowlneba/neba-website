using Neba.Api.Contracts.Tournaments.CreateTournament;

namespace Neba.Api.Contracts.Tournaments.EditTournamentLogo;

/// <summary>
/// Replaces or removes a tournament's logo.
/// </summary>
public sealed record EditTournamentLogoRequest
{
    /// <summary>
    /// The ULID string identifying the tournament whose logo changes.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The already-uploaded logo to use, or <see langword="null"/> to remove the current logo.
    /// </summary>
    public required TournamentLogoInput? Logo { get; init; }
}
