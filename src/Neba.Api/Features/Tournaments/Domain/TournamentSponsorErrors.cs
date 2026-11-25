using ErrorOr;

namespace Neba.Api.Features.Tournaments.Domain;

internal static class TournamentSponsorErrors
{
    public static readonly Error NegativeSponsorshipAmount = Error.Validation(
        code: "TournamentSponsor.NegativeSponsorshipAmount",
        description: "Sponsorship amount must be zero or greater.");

    public static readonly Error SponsorshipAmountTooLarge = Error.Validation(
        code: "TournamentSponsor.SponsorshipAmountTooLarge",
        description: "Sponsorship amount must be $99,999.99 or less.");
}