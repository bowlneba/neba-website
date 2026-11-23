using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Series.GetSeriesAdvancers;

namespace Neba.Api.Features.Series.GetSeriesAdvancers;

internal sealed class GetSeriesAdvancersSummary : Summary<GetSeriesAdvancersEndpoint>
{
    public GetSeriesAdvancersSummary()
    {
        Summary = "Gets the bowlers who advanced out of each Stage of a Series.";
        Description = "Public. Returns names only, with no status, override or version data. A Qualifying or Survivor Stage appears once it is Official. A Placement Stage appears once computed, when the Series has a Finale, and lists the finalists.";

        Response(200, "The settled Stages and who advanced out of them.",
            contentType: MediaTypeNames.Application.Json,
            example: new SeriesAdvancersResponse
            {
                Stages =
                [
                    new StageAdvancersResponse
                    {
                        StageId = "01JSTX1234567890ABCDEFGHIL",
                        Order = 1,
                        Kind = "Qualifying",
                        Advancers =
                        [
                            new AdvancerResponse
                            {
                                BowlerId = "01JSTX1234567890ABCDEFGHIN",
                                BowlerName = "Dave Smith",
                                Place = 1,
                                IsBackfill = false,
                            },
                        ],
                    },
                ],
            });

        Response(400, "The request is structurally invalid.");
        Response(404, "Series not found.");
    }
}
