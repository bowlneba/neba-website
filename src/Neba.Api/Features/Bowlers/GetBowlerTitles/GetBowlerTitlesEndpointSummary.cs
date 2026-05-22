using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Bowlers.GetBowlerTitles;

namespace Neba.Api.Features.Bowlers.GetBowlerTitles;

internal sealed class GetBowlerTitlesEndpointSummary
    : Summary<GetBowlerTitlesEndpoint>
{
    public GetBowlerTitlesEndpointSummary()
    {
        Summary = "Gets titles won by a bowler.";
        Description = "Retrieves all tournament titles won by a specific bowler, including Hall of Fame status and details for each tournament.";

        Response(200, "The bowler's titles.",
            contentType: MediaTypeNames.Application.Json,
            example: new BowlerTitlesResponse
            {
                BowlerName = "Dave Smith",
                HallOfFame = true,
                Titles =
                [
                    new BowlerTitleResponse
                    {
                        TournamentId = "01000000000000000000000001",
                        TournamentName = "NEBA Singles",
                        TournamentDate = new DateOnly(2018, 3, 10),
                        TournamentType = "Singles",
                    },
                ],
            });

        Response(400, "Invalid bowler ID — must be a 26-character ULID.");
        Response(404, "Bowler not found.");
    }
}
