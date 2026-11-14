using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Bowlers.SearchBowlers;

namespace Neba.Api.Features.Bowlers.SearchBowlers;

internal sealed class SearchBowlersSummary
    : Summary<SearchBowlersEndpoint>
{
    public SearchBowlersSummary()
    {
        Summary = "Searches bowlers by name.";
        Description = "Finds bowlers whose name contains the given text, ignoring case. The text must be at least 3 characters. "
            + "Returns at most 20 bowlers, ordered by last then first name, each with the bowler ID, display name and USBC ID. "
            + "A bowler with no USBC ID on file has a null USBC ID. This endpoint has its own, tighter rate limit.";

        ExampleRequest = new SearchBowlersRequest { Name = "smith" };

        Response(200, "The matching bowlers; empty when none match.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<BowlerSearchResponse>
            {
                Items =
                [
                    new BowlerSearchResponse
                    {
                        BowlerId = "01000000000000000000000001",
                        Name = "Dave Smith",
                        UsbcId = "4821-09317"
                    },
                    new BowlerSearchResponse
                    {
                        BowlerId = "01000000000000000000000002",
                        Name = "Dana Smith"
                    }
                ]
            });

        Response(400, "The name is missing, shorter than 3 characters, or longer than 100.");
        Response(429, "Too many searches from this address; retry after the time in the Retry-After header.");
    }
}
