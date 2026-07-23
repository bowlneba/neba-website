namespace Neba.Api.Contracts.Tournaments.AddTournamentSponsor;

/// <summary>
/// Adds a sponsor to a tournament.
/// </summary>
public sealed record AddTournamentSponsorRequest
{
    /// <summary>
    /// The ULID string identifying the tournament to add the sponsor to.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The sponsorship fields to add.
    /// </summary>
    public required AddTournamentSponsorInput Sponsor { get; init; }
}
