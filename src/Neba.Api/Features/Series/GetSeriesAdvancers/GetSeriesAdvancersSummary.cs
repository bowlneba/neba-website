using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Series.GetSeriesAdvancers;

namespace Neba.Api.Features.Series.GetSeriesAdvancers;

internal sealed class GetSeriesAdvancersSummary : Summary<GetSeriesAdvancersEndpoint>
{
    public GetSeriesAdvancersSummary()
    {
        Summary = "Gets the bowlers who advanced out of each tournament of a Series.";
        Description = "Public. Returns names only, with no status, override or version data. A Qualifying or Survivor tournament appears once it is Official. A Placement tournament appears once Official and lists every position, marking the finalists when the Series has a Finale.";

        Response(200, "The settled tournaments and who advanced out of them. A tournament shows once it is Official.",
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
                                Advanced = true,
                            },
                        ],
                    },
                ],
            });

        Response(400, "The request is structurally invalid.");
        Response(404, "Series not found.");
    }
}