using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Awards;

namespace Neba.Api.Awards.ListHighBlockAwards;

internal sealed class ListHighBlockAwardsSummary
    : Summary<ListHighBlockAwardsEndpoint>
{
    public ListHighBlockAwardsSummary()
    {
        Summary = "Lists all High Block awards.";
        Description = "Retrieves all High Block awards across all seasons, including the recipient, season, and qualifying block score.";

        Response(200, "The list of High Block awards.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<HighBlockAwardResponse>
            {
                Items =
                [
                    new HighBlockAwardResponse
                    {
                        Season = "2024-2025",
                        BowlerName = "John Doe",
                        Score = 1350,
                    },
                ],
            });
    }
}
