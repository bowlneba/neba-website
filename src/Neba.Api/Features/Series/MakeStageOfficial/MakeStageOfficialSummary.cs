using FastEndpoints;

using Neba.Api.Contracts.Series.MakeStageOfficial;

namespace Neba.Api.Features.Series.MakeStageOfficial;

internal sealed class MakeStageOfficialSummary : Summary<MakeStageOfficialEndpoint>
{
    public MakeStageOfficialSummary()
    {
        Summary = "Marks a reviewed Stage Official.";
        Description = "Settles a Computed Qualifying or Survivor Stage so the public page shows who advanced. The Stage must not be stale, every earlier Qualifying and Survivor Stage must be Official, and the last Qualifying Stage needs the Backfill first. Marking an Official Stage again does nothing. Requires the Series.MakeStageOfficial permission.";

        ExampleRequest = new MakeStageOfficialRequest
        {
            Id = "01JSTX1234567890ABCDEFGHIJ",
            StageId = "01JSTX1234567890ABCDEFGHIK",
            Official = new MakeStageOfficialInput { Version = 3 },
        };

        Response(204, "The Stage is Official.");
        Response(400, "The request is structurally invalid.");
        Response(401, "The caller is not authenticated.");
        Response(403, "The caller lacks Series.MakeStageOfficial.");
        Response(404, "Series or Stage not found.");
        Response(409, "The Version is stale, advancement is running, or the Stage is not ready (not computed, stale, an earlier Stage is not Official, or the Backfill has not run).");
        Response(422, "The Stage is a Placement Stage, which never becomes Official.");
    }
}
