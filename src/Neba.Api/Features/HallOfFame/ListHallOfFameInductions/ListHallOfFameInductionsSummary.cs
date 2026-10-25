using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.HallOfFame.ListHallOfFameInductions;

namespace Neba.Api.Features.HallOfFame.ListHallOfFameInductions;

internal sealed class ListHallOfFameInductionsSummary
    : Summary<ListHallOfFameInductionsEndpoint>
{
    public ListHallOfFameInductionsSummary()
    {
        Summary = "Lists all Hall of Fame inductions.";
        Description = "Retrieves all Hall of Fame inductions, including the inducted bowler, year, categories, photo, and the photo's focal point and zoom.";

#pragma warning disable S1075 // URIs should not be hardcoded
        Response(200, "The list of Hall of Fame inductions.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<HallOfFameInductionResponse>
            {
                Items =
                [
                    new HallOfFameInductionResponse
                    {
                        Id = "01JZ8K3M4N5P6Q7R8S9T0V1W2X",
                        Year = 2024,
                        BowlerName = "John Doe",
                        Categories = ["Superior Performance", "Meritorious Service"],
                        PhotoUri = null,
                    },
                    new HallOfFameInductionResponse
                    {
                        Id = "01JZ8K3M4N5P6Q7R8S9T0V1W3Y",
                        Year = 2019,
                        BowlerName = "Jane Smith",
                        Categories = ["Meritorious Service"],
                        PhotoUri = new Uri("https://files.bowlneba.com/hall-of-fame/jane-smith.jpg", UriKind.Absolute),
                        PhotoFocalX = 42,
                        PhotoFocalY = 28,
                        PhotoZoom = 150,
                    }
                ],
            });
#pragma warning restore S1075 // URIs should not be hardcoded
    }
}