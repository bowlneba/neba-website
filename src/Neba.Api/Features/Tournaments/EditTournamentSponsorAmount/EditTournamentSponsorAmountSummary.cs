using FastEndpoints;

namespace Neba.Api.Features.Tournaments.EditTournamentSponsorAmount;

internal sealed class EditTournamentSponsorAmountSummary : Summary<EditTournamentSponsorAmountEndpoint>
{
    public EditTournamentSponsorAmountSummary()
    {
        Summary = "Changes a tournament sponsor's amount.";
        Description = "Updates the sponsorship amount of a sponsor already attached to a tournament, without removing and re-adding it. Requires the Tournaments.ManageSponsors permission.";

        Response(204, "Sponsorship amount updated.");
        Response(400, "Tournament ID, sponsor ID or sponsorship amount failed structural validation.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Tournaments.ManageSponsors permission.");
        Response(404, "Tournament was not found.");
        Response(409, "The specified sponsor is not attached to this tournament.");
        Response(422, "The sponsorship amount is not valid.");
    }
}
