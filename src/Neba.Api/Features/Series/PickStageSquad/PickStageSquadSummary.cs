using FastEndpoints;

using Neba.Api.Contracts.Series.PickStageSquad;

namespace Neba.Api.Features.Series.PickStageSquad;

internal sealed class PickStageSquadSummary : Summary<PickStageSquadEndpoint>
{
    public PickStageSquadSummary()
    {
        Summary = "Picks a Survivor Stage's squad.";
        Description = "Sets the squad whose scores decide the Survivor Stage, then recomputes from that Stage before the response. Works in any Stage status. Requires the Series.EditSeries permission.";

        ExampleRequest = new PickStageSquadRequest
        {
            Id = "01JSTX1234567890ABCDEFGHIJ",
            StageId = "01JSTX1234567890ABCDEFGHIK",
            Squad = new PickStageSquadInput
            {
                SquadId = "01JSTX1234567890ABCDEFGHIL",
                Version = 3,
            },
        };

        Response(204, "The squad was saved.");
        Response(400, "The request is structurally invalid.");
        Response(401, "The caller is not authenticated.");
        Response(403, "The caller lacks Series.EditSeries.");
        Response(404, "Series or Stage not found.");
        Response(409, "The Version is stale or advancement is running.");
        Response(422, "The Stage is not a Survivor Stage, or the squad is not in its tournament.");
    }
}