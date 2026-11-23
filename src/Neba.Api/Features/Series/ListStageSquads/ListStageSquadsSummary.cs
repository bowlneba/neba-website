using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Series.ListStageSquads;

namespace Neba.Api.Features.Series.ListStageSquads;

internal sealed class ListStageSquadsSummary : Summary<ListStageSquadsEndpoint>
{
    public ListStageSquadsSummary()
    {
        Summary = "Lists the squads of a Stage's tournament.";
        Description = "Lists the squads of the Stage's tournament in start order, with Eastern date and start time, whether each has scores and whether the Stage uses it. For the squad picker. Requires the Series.EditSeries permission.";

        Response(200, "The squads.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<StageSquadSummaryResponse>
            {
                Items =
                [
                    new StageSquadSummaryResponse
                    {
                        SquadId = "01JSTX1234567890ABCDEFGHIO",
                        Date = new DateOnly(2025, 6, 28),
                        StartTime = new TimeOnly(9, 0),
                        HasScores = true,
                        IsSelected = true,
                    },
                ],
            });

        Response(400, "The request is structurally invalid.");
        Response(401, "The caller is not authenticated.");
        Response(403, "The caller lacks Series.EditSeries.");
        Response(404, "Series or Stage not found.");
    }
}