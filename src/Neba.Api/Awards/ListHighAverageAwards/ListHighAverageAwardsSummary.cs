using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Awards;

namespace Neba.Api.Awards.ListHighAverageAwards;

internal sealed class ListHighAverageAwardsSummary
    : Summary<ListHighAverageAwardsEndpoint>
{
    public ListHighAverageAwardsSummary()
    {
        Summary = "Lists all High Average awards.";
        Description = "Retrieves all High Average awards across all seasons, including the recipient, season, average, total games, and tournaments participated.";

        Response(200, "The list of High Average awards.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<HighAverageAwardResponse>
            {
                Items =
                [
                    new HighAverageAwardResponse
                    {
                        Season = "2024-2025",
                        BowlerName = "John Doe",
                        Average = 231.75m,
                        TotalGames = 80,
                        TournamentsParticipated = 10,
                    },
                ],
            });
    }
}