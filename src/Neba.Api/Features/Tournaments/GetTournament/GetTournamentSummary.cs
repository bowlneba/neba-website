using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Tournaments.GetTournament;

namespace Neba.Api.Features.Tournaments.GetTournament;

internal sealed class GetTournamentSummary : Summary<GetTournamentEndpoint>
{
    public GetTournamentSummary()
    {
        Summary = "Gets tournament details.";
        Description = "Retrieves full details for a tournament including schedule, entry info, bowling center, sponsors, oil patterns, and results (if available).";

        Response(200, "The tournament detail.",
            contentType: MediaTypeNames.Application.Json,
            example: new TournamentDetailResponse
            {
                Id = "01JSTX1234567890ABCDEFGHIJ",
                Name = "Spring Open",
                Season = "2024-2025 Season",
                StartDate = new DateOnly(2025, 3, 15),
                EndDate = new DateOnly(2025, 3, 15),
                StatsEligible = true,
                TournamentType = "Singles",
                EntryFee = 75.00m,
                RegistrationUrl = null,
                AddedMoney = 500m,
                Reservations = 24,
                EntryCount = 48,
                PatternLengthCategory = "Medium",
                PatternRatioCategory = "Medium",
                LogoUrl = null,
                BowlingCenter = new TournamentDetailBowlingCenterResponse
                {
                    Name = "Acme Lanes",
                    City = "Springfield",
                    State = "MA",
                },
                Sponsors = [],
                OilPatterns =
                [
                    new TournamentDetailOilPatternResponse
                    {
                        Name = "Kegel Broadway",
                        Length = 40,
                        Rounds = ["Qualifying", "Finals"],
                    },
                ],
                Winners = ["Jane Smith"],
                Results =
                [
                    new TournamentResultResponse
                    {
                        BowlerName = "Jane Smith",
                        Place = 1,
                        PrizeMoney = 500m,
                        Points = 10,
                        SideCutName = null,
                        SideCutIndicator = null,
                    },
                    new TournamentResultResponse
                    {
                        BowlerName = "John Doe",
                        Place = 2,
                        PrizeMoney = 250m,
                        Points = 5,
                        SideCutName = "Senior",
                        SideCutIndicator = "#0000FF",
                    },
                ],
            });

        Response(404, "Tournament not found.");
    }
}