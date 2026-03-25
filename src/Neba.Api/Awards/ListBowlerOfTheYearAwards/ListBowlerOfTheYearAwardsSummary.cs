using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Awards;

namespace Neba.Api.Awards.ListBowlerOfTheYearAwards;

internal sealed class ListBowlerOfTheYearAwardsSummary
    : Summary<ListBowlerOfTheYearAwardsEndpoint>
{
    public ListBowlerOfTheYearAwardsSummary()
    {
        Summary = "Lists all Bowler of the Year awards.";
        Description = "Retrieves all Bowler of the Year awards across all seasons, including the recipient, season, and category.";

        Response(200, "The list of Bowler of the Year awards.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<BowlerOfTheYearAwardResponse>
            {
                Items =
                [
                    new BowlerOfTheYearAwardResponse
                    {
                        Season = "2024-2025",
                        BowlerName = "John Doe",
                        Category = "Bowler of the Year",
                    },
                    new BowlerOfTheYearAwardResponse
                    {
                        Season = "2024-2025",
                        BowlerName = "Jane Smith",
                        Category = "Woman",
                    },
                ],
            });
    }
}
