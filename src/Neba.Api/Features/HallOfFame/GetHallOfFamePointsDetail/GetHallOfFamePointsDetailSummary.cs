using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.HallOfFame.GetHallOfFamePointsDetail;

namespace Neba.Api.Features.HallOfFame.GetHallOfFamePointsDetail;

internal sealed class GetHallOfFamePointsDetailSummary
    : Summary<GetHallOfFamePointsDetailEndpoint>
{
    public GetHallOfFamePointsDetailSummary()
    {
        Summary = "Gets one bowler's Hall of Fame points.";
        Description = "Retrieves one bowler's Hall of Fame point breakdown (each point-earning title or award with its date, points and running total, and a flag on the one that first reached the required points) "
            + "and the membership years that count toward the Hall of Fame. Points count toward Superior Performance only. "
            + "Works for inducted bowlers too. Membership years count from the membership data start year (1999), so the count can understate "
            + "when the response says the membership history is incomplete.";

        ExampleRequest = new GetHallOfFamePointsDetailRequest { BowlerId = "01JZ8K3M4N5P6Q7R8S9T0V1W2X" };

        Response(200, "The bowler's points breakdown and counted membership years.",
            contentType: MediaTypeNames.Application.Json,
            example: new HallOfFamePointsDetailResponse
            {
                BowlerId = "01JZ8K3M4N5P6Q7R8S9T0V1W2X",
                BowlerName = "John Doe",
                InductedCategories = [],
                TotalPoints = 18,
                SeasonsWithPoints = 2,
                Breakdown =
                [
                    new HallOfFamePointsEntryResponse { Source = "Bowler of the Year", Date = new DateOnly(2017, 12, 31), Points = 12, RunningTotal = 12, ReachedThreshold = false },
                    new HallOfFamePointsEntryResponse { Source = "Singles Title", Date = new DateOnly(2019, 3, 10), Points = 6, RunningTotal = 18, ReachedThreshold = false }
                ],
                MembershipYears =
                [
                    new HallOfFameMembershipYearResponse { Year = 2024, MembershipType = "Renewal" },
                    new HallOfFameMembershipYearResponse { Year = 2025, MembershipType = "Renewal" }
                ],
                MembershipHistoryComplete = false
            });

        Response(400, "The bowler ID is missing or is not 26 characters.");
        Response(404, "No bowler has the given ID.");
    }
}