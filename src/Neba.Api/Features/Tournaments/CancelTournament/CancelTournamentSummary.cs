using FastEndpoints;

namespace Neba.Api.Features.Tournaments.CancelTournament;

internal sealed class CancelTournamentSummary : Summary<CancelTournamentEndpoint>
{
    public CancelTournamentSummary()
    {
        Summary = "Marks a tournament cancelled.";
        Description = "Marks the tournament as having had no official NEBA event take place under this record. " +
                      "The tournament never counts toward stats or a title. Only available while the tournament is " +
                      "Scheduled; once finalized this action can't be undone. Requires the " +
                      "Tournaments.ManageTournamentStatus permission.";

        Response(204, "Tournament marked cancelled.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Tournaments.ManageTournamentStatus permission.");
        Response(404, "No tournament exists with the given ID.");
        Response(409, "The tournament has already been completed, truncated, or cancelled.");
    }
}