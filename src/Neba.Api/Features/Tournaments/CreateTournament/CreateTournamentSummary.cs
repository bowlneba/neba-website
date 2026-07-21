using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Tournaments.CreateTournament;

namespace Neba.Api.Features.Tournaments.CreateTournament;

internal sealed class CreateTournamentSummary : Summary<CreateTournamentEndpoint>
{
    public CreateTournamentSummary()
    {
        Summary = "Creates a tournament.";
        Description = "Creates a tournament. The season is derived automatically from the tournament's dates. Lane-condition categories are either derived from an existing oil pattern or entered manually, but not both. Requires the Tournaments.CreateTournament permission.";

#pragma warning disable S1075 // URIs should not be hardcoded
        Response(201, "Tournament created.",
            contentType: MediaTypeNames.Application.Json,
            example: new CreatedTournamentResponse
            {
                TournamentId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E"
            });
#pragma warning restore S1075 // URIs should not be hardcoded

        Response(400, "Name, tournament type, dates, entry fee, or pattern category failed structural validation.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Tournaments.CreateTournament permission.");
        Response(422, "No season is configured for the submitted dates, or the specified bowling center/oil pattern was not found.");
    }
}
