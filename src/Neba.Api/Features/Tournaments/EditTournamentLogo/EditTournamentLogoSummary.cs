using FastEndpoints;

using Neba.Api.Contracts.Tournaments.CreateTournament;
using Neba.Api.Contracts.Tournaments.EditTournamentLogo;

namespace Neba.Api.Features.Tournaments.EditTournamentLogo;

internal sealed class EditTournamentLogoSummary : Summary<EditTournamentLogoEndpoint>
{
    public EditTournamentLogoSummary()
    {
        Summary = "Replaces or removes a tournament's logo.";
        Description = "Sets the tournament's logo to a previously uploaded file, or clears it when the logo is null. Does not change any other tournament field. Requires the Tournaments.EditTournamentLogo permission.";

        // Id and Logo sit directly on the request. If a third logo host or a second field appears,
        // wrap them in an input object like the other command requests.
        ExampleRequest = new EditTournamentLogoRequest
        {
            Id = "01JZ8K3M4N5P6Q7R8S9T0V1W2X",
            Logo = new TournamentLogoInput
            {
                Container = "tournaments",
                Path = "logos/01JZ8K3M4N5P6Q7R8S9T0V1W2X.png",
                ContentType = "image/png",
                SizeInBytes = 48213
            }
        };

        Response(204, "Tournament logo updated.");
        Response(400, "Id or the logo fields failed structural validation.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Tournaments.EditTournamentLogo permission.");
        Response(404, "No tournament exists with the given Id.");
        Response(422, "The logo is not a file uploaded through the logo upload endpoint.");
    }
}
