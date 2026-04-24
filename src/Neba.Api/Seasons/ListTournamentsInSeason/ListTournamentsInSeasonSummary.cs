using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Seasons.ListTournamentsInSeason;

namespace Neba.Api.Seasons.ListTournamentsInSeason;

internal sealed class ListTournamentsInSeasonSummary
    : Summary<ListTournamentsInSeasonEndpoint>
{
    public ListTournamentsInSeasonSummary()
    {
        Summary = "Lists all tournaments in a season.";
        Description = "Retrieves all tournaments for the given season, including dates, entry fees, bowling center, sponsors, and oil patterns.";

        Response(200, "The list of tournaments.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<SeasonTournamentResponse>
            {
                Items =
                [
                    new SeasonTournamentResponse
                    {
                        Id = "01JSTX1234567890ABCDEFGHIJ",
                        Name = "Spring Open",
                        StartDate = new DateOnly(2025, 3, 15),
                        EndDate = new DateOnly(2025, 3, 15),
                        TournamentType = "Singles",
                        EntryFee = 75.00m,
                        RegistrationUrl = null,
                        AddedMoney = 500m,
                        Reservations = 24,
                        PatternLengthCategory = "Medium",
                        PatternRatioCategory = "Medium",
                        LogoUrl = null,
                        Winners = ["Jane Smith"],
                        BowlingCenter = new TournamentBowlingCenterResponse
                        {
                            Name = "Acme Lanes",
                            City = "Springfield",
                            State = "MA",
                        },
                        Sponsors = [],
                        OilPatterns =
                        [
                            new TournamentOilPatternResponse
                            {
                                Name = "Kegel Broadway",
                                Length = 40,
                                Volume = 25.0m,
                                LeftRatio = 3.2m,
                                RightRatio = 3.1m,
                                KegelId = null,
                                Rounds = ["Qualifying", "Finals"],
                            },
                        ],
                    },
                ],
            });

        Response(400, "Invalid season ID.");
    }
}
