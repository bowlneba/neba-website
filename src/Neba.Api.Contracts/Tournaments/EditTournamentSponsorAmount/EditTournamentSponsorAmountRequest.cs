namespace Neba.Api.Contracts.Tournaments.EditTournamentSponsorAmount;

/// <summary>
/// Changes the sponsorship amount of a sponsor attached to a tournament.
/// </summary>
public sealed record EditTournamentSponsorAmountRequest
{
    /// <summary>
    /// The ULID string identifying the tournament.
    /// </summary>
    /// <example>01JX0000000000000000000001</example>
    public required string Id { get; init; }

    /// <summary>
    /// The ULID string identifying the attached sponsor.
    /// </summary>
    /// <example>01JX0000000000000000000101</example>
    public required string SponsorId { get; init; }

    /// <summary>
    /// The new sponsorship amount.
    /// </summary>
    public required EditTournamentSponsorAmountInput Sponsor { get; init; }
}
