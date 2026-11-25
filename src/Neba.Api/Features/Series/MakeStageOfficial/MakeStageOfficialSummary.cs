using FastEndpoints;

using Neba.Api.Contracts.Series.MakeStageOfficial;

namespace Neba.Api.Features.Series.MakeStageOfficial;

internal sealed class MakeStageOfficialSummary : Summary<MakeStageOfficialEndpoint>
{
    public MakeStageOfficialSummary()
    {
        Summary = "Marks a reviewed tournament Official.";
        Description = "Settles a Computed Qualifying or Survivor tournament so the public page shows who advanced. The tournament must not be stale, every earlier Qualifying and Survivor tournament must be Official, and the last Qualifying tournament needs the Bowler of the Year point list first. Marking an Official tournament again does nothing. Requires the Series.MakeStageOfficial permission.";

        ExampleRequest = new MakeStageOfficialRequest
        {
            Id = "01JSTX1234567890ABCDEFGHIJ",
            StageId = "01JSTX1234567890ABCDEFGHIK",
            Official = new MakeStageOfficialInput { Version = 3 },
        };

        Response(204, "The tournament is Official.");
        Response(400, "The request is structurally invalid.");
        Response(401, "The caller is not authenticated.");
        Response(403, "The caller lacks Series.MakeStageOfficial.");
        Response(404, "Series or tournament not found.");
        Response(409, "The Version is stale, advancement is running, or the tournament is not ready (not computed, stale, an earlier tournament is not Official, or the Bowler of the Year point list has not been used).");
    }
}