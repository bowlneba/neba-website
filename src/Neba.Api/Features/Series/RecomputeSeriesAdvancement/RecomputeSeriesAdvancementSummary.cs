using FastEndpoints;

using Neba.Api.Contracts.Series.RecomputeSeriesAdvancement;

namespace Neba.Api.Features.Series.RecomputeSeriesAdvancement;

internal sealed class RecomputeSeriesAdvancementSummary : Summary<RecomputeSeriesAdvancementEndpoint>
{
    public RecomputeSeriesAdvancementSummary()
    {
        Summary = "Recomputes a Series' advancement from a Stage.";
        Description = "Recomputes the Stage and every later Stage from stored results and overrides, before the response. A recompute that reaches official Stages needs ConfirmResetOfficial; it resets them to Computed and clears Stale. Requires the Series.EditSeries permission.";

        ExampleRequest = new RecomputeSeriesAdvancementRequest
        {
            Id = "01JSTX1234567890ABCDEFGHIJ",
            StageId = "01JSTX1234567890ABCDEFGHIK",
            Recomputation = new RecomputeSeriesAdvancementInput
            {
                Version = 3,
                ConfirmResetOfficial = false,
            },
        };

        Response(204, "The Series was recomputed (or nothing changed).");
        Response(400, "The request is structurally invalid.");
        Response(401, "The caller is not authenticated.");
        Response(403, "The caller lacks Series.EditSeries.");
        Response(404, "Series or Stage not found.");
        Response(409, "The Version is stale, advancement is running, or official Stages need confirmation (the errors list them as Series.Stages[n]).");
    }
}