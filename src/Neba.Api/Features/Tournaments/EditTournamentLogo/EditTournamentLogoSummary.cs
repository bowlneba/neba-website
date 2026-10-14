using FastEndpoints;

namespace Neba.Api.Features.Tournaments.EditTournamentLogo;

internal sealed class EditTournamentLogoSummary : Summary<EditTournamentLogoEndpoint>
{
    public EditTournamentLogoSummary()
    {
        Summary = "Replaces or removes a tournament's logo.";
        Description = "Sets the tournament's logo to a previously uploaded file, or clears it when the logo is null. Does not change any other tournament field. Requires the Tournaments.EditTournament or Tournaments.EditTournamentLogo permission.";

        Response(204, "Tournament logo updated.");
        Response(400, "Id or the logo fields failed structural validation.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user has neither the Tournaments.EditTournament nor the Tournaments.EditTournamentLogo permission.");
        Response(404, "No tournament exists with the given Id.");
    }
}
