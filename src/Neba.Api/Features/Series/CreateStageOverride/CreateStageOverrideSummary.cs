using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Series;
using Neba.Api.Contracts.Series.CreateStageOverride;

namespace Neba.Api.Features.Series.CreateStageOverride;

internal sealed class CreateStageOverrideSummary : Summary<CreateStageOverrideEndpoint>
{
    public CreateStageOverrideSummary()
    {
        Summary = "Adds a bowler to a Qualifying Stage or clears a slot.";
        Description = "Kind 1 (Add) puts any bowler with a result row in the Stage's tournament in the field; the field may exceed the target. Kind 2 (Clear) vacates the slot the bowler holds, so the Backfill refills it, and bars the bowler from the Backfill. The Stage must be Computed. The Series recomputes before the response. Requires the Series.ManageAdvancement permission.";

        ExampleRequest = new CreateStageOverrideRequest
        {
            Id = "01JSTX1234567890ABCDEFGHIJ",
            StageId = "01JSTX1234567890ABCDEFGHIK",
            StageOverride = new CreateStageOverrideInput
            {
                Kind = OverrideKindValues.Add,
                BowlerId = "01JSTX1234567890ABCDEFGHIN",
                Reason = "Entry confirmed by the tournament director.",
                Version = 3,
            },
        };

        Response<CreatedStageOverrideResponse>(201, "The override was saved and the Series recomputed.",
            contentType: MediaTypeNames.Application.Json,
            example: new CreatedStageOverrideResponse
            {
                OverrideId = "01JSTX1234567890ABCDEFGHIO",
                Version = 4,
            });

        Response(400, "The request is structurally invalid.");
        Response(401, "The caller is not authenticated.");
        Response(403, "The caller lacks Series.ManageAdvancement.");
        Response(404, "Series or Stage not found.");
        Response(409, "The Version is stale, advancement is running, the Stage is not Computed, the bowler is already in the field, or the bowler already has an override on this Stage.");
        Response(422, "The Stage is not a Qualifying Stage, the bowler has no result row, or the bowler holds no filled slot.");
    }
}