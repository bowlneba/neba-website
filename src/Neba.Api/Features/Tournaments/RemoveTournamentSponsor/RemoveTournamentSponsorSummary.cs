using FastEndpoints;

namespace Neba.Api.Features.Tournaments.RemoveTournamentSponsor;

internal sealed class RemoveTournamentSponsorSummary : Summary<RemoveTournamentSponsorEndpoint>
{
    public RemoveTournamentSponsorSummary()
    {
        Summary = "Removes a sponsor from a tournament.";
        Description = "Detaches a sponsor from a tournament. Does not affect the sponsor's own profile. Requires the Tournaments.ManageSponsors permission.";

        Response(204, "Sponsor removed.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Tournaments.ManageSponsors permission.");
        Response(404, "Tournament was not found.");
        Response(409, "The specified sponsor is not attached to this tournament.");
    }
}