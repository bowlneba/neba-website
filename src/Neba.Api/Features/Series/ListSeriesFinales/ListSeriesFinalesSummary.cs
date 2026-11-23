using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Series.GetSeries;
using Neba.Api.Contracts.Series.ListSeriesFinales;

namespace Neba.Api.Features.Series.ListSeriesFinales;

internal sealed class ListSeriesFinalesSummary : Summary<ListSeriesFinalesEndpoint>
{
    public ListSeriesFinalesSummary()
    {
        Summary = "Lists every Series Finale, soonest first.";
        Description = "Lists the Finale of every Series that has one, soonest first (date, then start time), with the Series' name and logo and the host center.";

        Response(200, "The Finales.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<SeriesFinaleSummaryResponse>
            {
                Items =
                [
                    new SeriesFinaleSummaryResponse
                    {
                        SeriesId = "01JSTX1234567890ABCDEFGHIJ",
                        SeriesName = "2025 Vincent Hall Memorial Series",
                        SeasonId = "01JSTX1234567890ABCDEFGHIK",
                        Finale = new SeriesFinaleResponse
                        {
                            FinalistCount = 4,
                            Date = new DateOnly(2025, 9, 27),
                            StartTime = new TimeOnly(14, 0),
                            BowlingCenterId = "01948",
                            BowlingCenterName = "Callahan's Bowl-O-Rama",
                            BowlingCenterCity = "Seekonk",
                            BowlingCenterState = "MA",
                        },
                    },
                ],
            });
    }
}