using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Series.GetSeries;

namespace Neba.Api.Features.Series.GetSeries;

internal sealed class GetSeriesSummary : Summary<GetSeriesEndpoint>
{
    public GetSeriesSummary()
    {
        Summary = "Gets a Series and its Stage schedule.";
        Description = "Retrieves a Series with its season, Finale, logo and ordered Stages, each linked to its tournament.";

        Response(200, "The Series detail.",
            contentType: MediaTypeNames.Application.Json,
            example: new SeriesDetailResponse
            {
                Id = "01JSTX1234567890ABCDEFGHIJ",
                Name = "2025 Vincent Hall Memorial Series",
                SeasonId = "01JSTX1234567890ABCDEFGHIK",
                SeasonDescription = "2025 Season",
                SeasonStartDate = new DateOnly(2025, 1, 1),
                SeasonEndDate = new DateOnly(2025, 12, 31),
                IsLocked = false,
                Finale = new SeriesFinaleResponse
                {
                    FinalistCount = 4,
                    Date = new DateOnly(2025, 9, 27),
                    StartTime = new TimeOnly(14, 0),
                },
                Stages =
                [
                    new SeriesStageResponse
                    {
                        Id = "01JSTX1234567890ABCDEFGHIL",
                        Order = 1,
                        Kind = "Qualifying",
                        Status = "Pending",
                        AdvancingCount = 8,
                        TournamentId = "01JSTX1234567890ABCDEFGHIM",
                        TournamentName = "Westgate Lanes",
                        TournamentStartDate = new DateOnly(2025, 5, 31),
                        TournamentEndDate = new DateOnly(2025, 5, 31),
                        TournamentType = "Singles",
                        TeamSize = 1,
                    },
                ],
            });

        Response(404, "Series not found.");
    }
}