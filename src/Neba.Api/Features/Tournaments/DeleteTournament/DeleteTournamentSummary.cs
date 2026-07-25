using FastEndpoints;

namespace Neba.Api.Features.Tournaments.DeleteTournament;

internal sealed class DeleteTournamentSummary : Summary<DeleteTournamentEndpoint>
{
    public DeleteTournamentSummary()
    {
        Summary = "Deletes a tournament.";
        Description = "Permanently deletes the tournament, its sponsor links, and oil pattern assignments. " +
                      "Returns 204 whether or not a tournament with the given ID existed, so callers cannot use this " +
                      "endpoint to probe for tournament existence. Refuses with 409 if the tournament has recorded " +
                      "championship, entry, or result history. Requires the Tournaments.DeleteTournament permission.";

        Response(204, "Tournament deleted, or no tournament existed with the given ID.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Tournaments.DeleteTournament permission.");
        Response(409, "The tournament has recorded championship, entry, or result history and cannot be deleted.");
    }
}