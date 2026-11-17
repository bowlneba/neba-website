using FastEndpoints;

namespace Neba.Api.Features.Series.EditSeries;

internal sealed class EditSeriesSummary : Summary<EditSeriesEndpoint>
{
    public EditSeriesSummary()
    {
        Summary = "Saves a Series.";
        Description = "Replaces a Series' name, logo, ordered Stages and Finale in one save. The season cannot change. While the Series is locked only the name, logo and the Finale's date, start time and center can change. Requires the Series.EditSeries permission.";

        Response(204, "The Series was saved.");
        Response(400, "The request is structurally invalid.");
        Response(401, "The caller is not authenticated.");
        Response(403, "The caller lacks Series.EditSeries, or is denied Series.EditSeriesLogo and changed the logo.");
        Response(404, "Series not found.");
        Response(409, "The Series is locked, or a tournament is already a Stage in another Series.");
        Response(422, "A Stage or the Finale breaks a Series rule. Each error names the Stage or field.");
    }
}
