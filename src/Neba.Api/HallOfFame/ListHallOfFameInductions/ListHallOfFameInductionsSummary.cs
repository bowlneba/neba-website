using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.HallOfFame.ListHallOfFameInductions;

namespace Neba.Api.HallOfFame.ListHallOfFameInductions;

internal sealed class ListHallOfFameInductionsSummary
    : Summary<ListHallOfFameInductionsEndpoint>
{
    public ListHallOfFameInductionsSummary()
    {
        Summary = "Lists all Hall of Fame inductions.";
        Description = "Retrieves all Hall of Fame inductions, including the inducted bowler, year, categories, and photo.";

#pragma warning disable S1075 // URIs should not be hardcoded
        Response(200, "The list of Hall of Fame inductions.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<HallOfFameInductionResponse>
            {
                Items =
                [
                    new HallOfFameInductionResponse
                    {
                        Year = 2024,
                        BowlerName = "John Doe",
                        Categories = ["Superior Performance", "Meritorious Service"],
                        PhotoUri = null,
                    },
                    new HallOfFameInductionResponse
                    {
                        Year = 2019,
                        BowlerName = "Jane Smith",
                        Categories = ["Meritorious Service"],
                        PhotoUri = new Uri("https://files.bowlneba.com/hall-of-fame/jane-smith.jpg", UriKind.Absolute),
                    }
                ],
            });
#pragma warning restore S1075 // URIs should not be hardcoded
    }
}