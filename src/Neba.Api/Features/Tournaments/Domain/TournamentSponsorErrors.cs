using ErrorOr;

namespace Neba.Api.Features.Tournaments.Domain;

internal static class TournamentSponsorErrors
{
    public static readonly Error NegativeSponsorshipAmount = Error.Validation(
        code: "TournamentSponsor.NegativeSponsorshipAmount",
        description: "Sponsorship amount must be zero or greater.");
}