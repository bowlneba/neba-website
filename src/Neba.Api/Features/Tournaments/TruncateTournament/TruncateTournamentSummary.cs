using FastEndpoints;

namespace Neba.Api.Features.Tournaments.TruncateTournament;

internal sealed class TruncateTournamentSummary : Summary<TruncateTournamentEndpoint>
{
    public TruncateTournamentSummary()
    {
        Summary = "Marks a tournament truncated.";
        Description = "Marks the tournament as having been held but not finished as planned. The tournament may " +
                      "still count toward season stats but is never eligible for a title. Only available while the " +
                      "tournament is Scheduled; once finalized this action can't be undone. Requires the " +
                      "Tournaments.ManageTournamentStatus permission.";

        Response(204, "Tournament marked truncated.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Tournaments.ManageTournamentStatus permission.");
        Response(404, "No tournament exists with the given ID.");
        Response(409, "The tournament has already been completed, truncated, or cancelled.");
    }
}
