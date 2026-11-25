namespace Neba.Api.Contracts.Tournaments.EditTournamentSponsorAmount;

/// <summary>
/// The fields that can change on an attached tournament sponsor.
/// </summary>
public sealed record EditTournamentSponsorAmountInput
{
    /// <summary>
    /// The new sponsorship amount, in dollars.
    /// </summary>
    /// <example>1500.00</example>
    public required decimal SponsorshipAmount { get; init; }
}
