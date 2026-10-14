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
    /// The logo change to apply.
    /// </summary>
    public required EditTournamentLogoInput Input { get; init; }
}