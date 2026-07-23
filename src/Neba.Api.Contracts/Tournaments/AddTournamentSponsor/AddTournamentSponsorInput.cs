namespace Neba.Api.Contracts.Tournaments.AddTournamentSponsor;

/// <summary>
/// The fields required to attach a sponsor to a tournament.
/// </summary>
public sealed record AddTournamentSponsorInput
{
    /// <summary>
    /// The ULID string identifying the sponsor to attach.
    /// </summary>
    public required string SponsorId { get; init; }

    /// <summary>
    /// Whether this sponsor is the tournament's title sponsor. Only one sponsor per tournament may hold this designation.
    /// </summary>
    public required bool TitleSponsor { get; init; }

    /// <summary>
    /// The sponsorship amount, in dollars.
    /// </summary>
    public required decimal SponsorshipAmount { get; init; }
}