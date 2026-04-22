using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Seasons.ListSeasons;

namespace Neba.Api.Seasons.ListSeasons;

internal sealed class ListSeasonsSummary
    : Summary<ListSeasonsEndpoint>
{
    public ListSeasonsSummary()
    {
        Summary = "Lists all seasons.";
        Description = "Retrieves all seasons, including their identifier, description, and date range.";

        Response(200, "The list of seasons.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<SeasonResponse>
            {
                Items =
                [
                    new SeasonResponse
                    {
                        Id = "01JSTX1234567890ABCDEFGHIJ",
                        Description = "2024-2025 Season",
                        StartDate = new DateOnly(2024, 9, 1),
                        EndDate = new DateOnly(2025, 8, 31),
                    },
                ],
            });
    }
}
