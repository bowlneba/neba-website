using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Series.ListSeries;

namespace Neba.Api.Features.Series.ListSeries;

internal sealed class ListSeriesSummary : Summary<ListSeriesEndpoint>
{
    public ListSeriesSummary()
    {
        Summary = "Lists every Series, newest season first.";
        Description = "Lists every Series, newest season first, each with its next tournament (the first whose tournament hasn't ended).";

        Response(200, "The Series.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<SeriesSummaryResponse>
            {
                Items =
                [
                    new SeriesSummaryResponse
                    {
                        Id = "01JSTX1234567890ABCDEFGHIJ",
                        Name = "2025 Vincent Hall Memorial Series",
                        SeasonId = "01JSTX1234567890ABCDEFGHIK",
                        SeasonDescription = "2025 Season",
                        SeasonStartDate = new DateOnly(2025, 1, 1),
                        SeasonEndDate = new DateOnly(2025, 12, 31),
                        QualifyingStageCount = 8,
                        SurvivorStageCount = 2,
                        PlacementStageCount = 1,
                        IsLocked = false,
                        StageTournamentIds = ["01JSTX1234567890ABCDEFGHIL"],
                        NextStage = new SeriesNextStageResponse
                        {
                            Kind = "Qualifying",
                            TournamentId = "01JSTX1234567890ABCDEFGHIL",
                            TournamentName = "Westgate Lanes",
                            TournamentStartDate = new DateOnly(2025, 5, 31),
                            TournamentEndDate = new DateOnly(2025, 5, 31),
                        },
                    },
                ],
            });
    }
}