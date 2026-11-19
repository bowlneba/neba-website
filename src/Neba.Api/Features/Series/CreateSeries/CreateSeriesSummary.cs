using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Series.CreateSeries;

namespace Neba.Api.Features.Series.CreateSeries;

internal sealed class CreateSeriesSummary : Summary<CreateSeriesEndpoint>
{
    public CreateSeriesSummary()
    {
        Summary = "Creates a Series.";
        Description = "Creates a Series for a season with its ordered Stages and an optional Finale in one save. Requires the Series.CreateSeries permission.";

        Response<CreatedSeriesResponse>(201, "The Series was created.",
            contentType: MediaTypeNames.Application.Json,
            example: new CreatedSeriesResponse { SeriesId = "01JSTX1234567890ABCDEFGHIJ" });

        Response(400, "The request is structurally invalid.");
        Response(401, "The caller is not authenticated.");
        Response(403, "The caller lacks Series.CreateSeries.");
        Response(409, "A tournament is already a Stage in another Series.");
        Response(422, "The season was not found or a Stage or the Finale breaks a Series rule. Each error names the Stage or field.");
    }
}