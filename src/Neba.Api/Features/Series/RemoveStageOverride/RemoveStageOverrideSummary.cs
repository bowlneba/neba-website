using FastEndpoints;

namespace Neba.Api.Features.Series.RemoveStageOverride;

internal sealed class RemoveStageOverrideSummary : Summary<RemoveStageOverrideEndpoint>
{
    public RemoveStageOverrideSummary()
    {
        Summary = "Removes a Stage override.";
        Description = "Removes an Add or Clear override and recomputes the Series before the response. Removing an override that is already gone succeeds without checking the Version. Pass the Version from the last GET as the version query value. Requires the Series.ManageAdvancement permission.";

        Response(204, "The override was removed, or it was already gone.");
        Response(400, "The request is structurally invalid (including a missing version).");
        Response(401, "The caller is not authenticated.");
        Response(403, "The caller lacks Series.ManageAdvancement.");
        Response(404, "Series or Stage not found.");
        Response(409, "The Version is stale (checked only when the override exists), advancement is running, or the Stage is official.");
    }
}