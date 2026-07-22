using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.OilPatterns.ListOilPatterns;

namespace Neba.Api.Features.Tournaments.ListOilPatterns;

internal sealed class ListOilPatternsSummary
    : Summary<ListOilPatternsEndpoint>
{
    public ListOilPatternsSummary()
    {
        Summary = "Lists all oil patterns.";
        Description = "Retrieves a summary list of all oil patterns, including their derived lane-condition categories.";

        Response(200, "The list of oil patterns.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<OilPatternSummaryResponse>
            {
                Items =
                [
                    new OilPatternSummaryResponse
                    {
                        OilPatternId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E",
                        Name = "Dragon",
                        Length = 40,
                        Volume = 25.0m,
                        LeftRatio = 3.0m,
                        RightRatio = 3.0m,
                        KegelId = null,
                        LengthCategory = "Medium",
                        RatioCategory = "Challenge"
                    }
                ]
            });
    }
}
