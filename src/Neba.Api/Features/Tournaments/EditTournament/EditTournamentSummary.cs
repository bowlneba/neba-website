using FastEndpoints;

namespace Neba.Api.Features.Tournaments.EditTournament;

internal sealed class EditTournamentSummary : Summary<EditTournamentEndpoint>
{
    public EditTournamentSummary()
    {
        Summary = "Edits a tournament.";
        Description = "Replaces the tournament's editable fields (name, type, dates, venue, entry fee, added money, registration URL, logo, and oil pattern/lane-condition categories). The season is re-derived from the submitted dates. Requires the Tournaments.EditTournament permission.";

        Response(204, "Tournament updated.");
        Response(400, "Id, Name, Tournament Type, dates, entry fee, added money, or a category field failed structural validation.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Tournaments.EditTournament permission.");
        Response(404, "No tournament exists with the given Id.");
        Response(409, "A domain conflict occurred.");
        Response(422, "The submitted dates don't fall within any configured season, or the specified bowling center/oil pattern was not found.");
    }
}
