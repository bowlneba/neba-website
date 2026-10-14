using Neba.Api.Contracts.Tournaments.CreateTournament;

namespace Neba.Api.Contracts.Tournaments.EditTournamentLogo;

/// <summary>
/// The logo change to apply to a tournament.
/// </summary>
public sealed record EditTournamentLogoInput
{
    /// <summary>
    /// The already-uploaded logo to use, or <see langword="null"/> to remove the current logo.
    /// </summary>
    public required TournamentLogoInput? Logo { get; init; }
}