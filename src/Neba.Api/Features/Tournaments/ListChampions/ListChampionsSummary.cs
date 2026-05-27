using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Tournaments.ListChampions;

namespace Neba.Api.Features.Tournaments.ListChampions;

internal sealed class ListChampionsSummary
    : Summary<ListChampionsEndpoint>
{
    public ListChampionsSummary()
    {
        Summary = "Lists all tournament champions.";
        Description = "Retrieves all historical tournament champions grouped by tournament, including bowler name, Hall of Fame status, and tournament details.";

        Response(200, "The list of tournament champions.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<TournamentChampionResponse>
            {
                Items =
                [
                    new TournamentChampionResponse
                    {
                        TournamentId = "01JSTX1234567890ABCDEFGHIJ",
                        TournamentName = "NEBA Singles Classic",
                        TournamentDate = new DateOnly(2024, 11, 1),
                        TournamentType = "Singles",
                        Champions =
                        [
                            new ChampionResponse
                            {
                                BowlerId = "01JSTX0987654321ZYXWVUTSRQ",
                                BowlerName = "Jane Smith",
                                HallOfFame = true,
                            },
                        ],
                    },
                ],
            });
    }
}