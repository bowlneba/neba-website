using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Series.ListStageResults;

namespace Neba.Api.Features.Series.ListStageResults;

internal sealed class ListStageResultsSummary : Summary<ListStageResultsEndpoint>
{
    public ListStageResultsSummary()
    {
        Summary = "Lists the result rows of a Series tournament.";
        Description = "Lists every result row of the Series tournament, best place first, for the add-bowler picker. Empty until the tournament has results. Requires the Series.ManageAdvancement permission.";

        Response(200, "The result rows.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<StageResultSummaryResponse>
            {
                Items =
                [
                    new StageResultSummaryResponse
                    {
                        BowlerId = "01JSTX1234567890ABCDEFGHIN",
                        BowlerName = "Dave Smith",
                        Place = 1,
                    },
                ],
            });

        Response(400, "The request is structurally invalid.");
        Response(401, "The caller is not authenticated.");
        Response(403, "The caller lacks Series.ManageAdvancement.");
        Response(404, "Series or tournament not found.");
    }
}