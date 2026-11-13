using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.HallOfFame.ListHallOfFamePoints;

namespace Neba.Api.Features.HallOfFame.ListHallOfFamePoints;

internal sealed class ListHallOfFamePointsSummary
    : Summary<ListHallOfFamePointsEndpoint>
{
    public ListHallOfFamePointsSummary()
    {
        Summary = "Lists Hall of Fame points for bowlers not inducted for Superior Performance.";
        Description = "Retrieves every bowler with at least one Hall of Fame point who is not inducted for Superior Performance, "
            + "with the point table and the Superior Performance thresholds. Points count toward Superior Performance only, so a bowler "
            + "inducted for Meritorious Service or Friend of NEBA still appears. Membership years count from the membership data start year. "
            + "Standings run points high to low, then by name; there is no rank.";

        Response(200, "The point table, thresholds, and each bowler's standing.",
            contentType: MediaTypeNames.Application.Json,
            example: new HallOfFamePointsResponse
            {
                Rules =
                [
                    new HallOfFamePointRuleResponse { Group = "Bowler of the Year Awards", Label = "Bowler of the Year", Points = 12 },
                    new HallOfFamePointRuleResponse { Group = "Tournament Titles", Label = "Singles Title", Points = 6 }
                ],
                RequiredPoints = 36,
                RequiredSeasonsWithPoints = 3,
                RequiredMembershipYears = 10,
                MembershipDataStartYear = 1999,
                Standings =
                [
                    new HallOfFamePointsStandingResponse
                    {
                        BowlerId = "01JZ8K3M4N5P6Q7R8S9T0V1W2X",
                        BowlerName = "John Doe",
                        TotalPoints = 42,
                        SeasonsWithPoints = 5,
                        MembershipYears = 13,
                        MembershipHistoryComplete = true,
                        Eligible = true,
                        MeetsPoints = true,
                        MeetsSeasons = true,
                        InductedCategories = []
                    }
                ]
            });
    }
}