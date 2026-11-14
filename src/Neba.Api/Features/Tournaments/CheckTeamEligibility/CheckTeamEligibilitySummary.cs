using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Tournaments.CheckTeamEligibility;

namespace Neba.Api.Features.Tournaments.CheckTeamEligibility;

internal sealed class CheckTeamEligibilitySummary
    : Summary<CheckTeamEligibilityEndpoint>
{
    public CheckTeamEligibilitySummary()
    {
        Summary = "Checks whether a team may enter a tournament.";
        Description = "Checks a doubles, trios, or baker team against Rules 401 to 403 (previous champions, Hall of Fame members, and, for over/under teams, age) at the tournament's start and end dates. "
            + "Pass the IDs of the bowlers found in the system as repeated bowlerIds query values, and the number of team members not found in the system as unknownBowlerCount; "
            + "together they must equal the team size. A team that matches the previous tournament's winners is a defending team and skips the other rules. "
            + "PBA and PWBA titles are not checked. Date of birth is never returned.";

        Response(200, "The answer for the team.",
            contentType: MediaTypeNames.Application.Json,
            example: new TeamEligibilityResponse
            {
                Outcome = "Ineligible",
                Reasons =
                [
                    new TeamEligibilityReasonResponse
                    {
                        Code = "Champions.TooMany",
                        Message = "A team may have at most 1 previous champion."
                    }
                ]
            });

        Response(400, "A tournament or bowler ID is not a 26-character ULID, a bowler appears twice, or a count is out of range.");
        Response(404, "The tournament, or one of the bowlers, was not found.");
        Response(422, "The tournament is not a team event, or the bowler count does not match its team size.");
    }
}
