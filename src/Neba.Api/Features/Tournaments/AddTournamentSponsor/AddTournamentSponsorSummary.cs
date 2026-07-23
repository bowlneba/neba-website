using FastEndpoints;

namespace Neba.Api.Features.Tournaments.AddTournamentSponsor;

internal sealed class AddTournamentSponsorSummary : Summary<AddTournamentSponsorEndpoint>
{
    public AddTournamentSponsorSummary()
    {
        Summary = "Adds a sponsor to a tournament.";
        Description = "Attaches an existing sponsor to a tournament with a sponsorship amount, optionally marking it the title sponsor. Requires the Tournaments.ManageSponsors permission.";

        Response(204, "Sponsor added.");
        Response(400, "Sponsor ID or sponsorship amount failed structural validation.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Tournaments.ManageSponsors permission.");
        Response(404, "Tournament was not found.");
        Response(409, "The sponsor is already attached to this tournament, or a title sponsor is already set.");
        Response(422, "The specified sponsor was not found.");
    }
}