using Neba.Api.Contracts.HallOfFame.ListHallOfFamePoints;

namespace Neba.TestFactory.HallOfFame;

public static class HallOfFamePointsResponseFactory
{
    public static HallOfFamePointsResponse Create(
        IReadOnlyCollection<HallOfFamePointRuleResponse>? rules = null,
        IReadOnlyCollection<HallOfFamePointsStandingResponse>? standings = null,
        int? requiredPoints = null,
        int? requiredSeasonsWithPoints = null,
        int? requiredMembershipYears = null,
        int? membershipDataStartYear = null)
        => new()
        {
            Rules = rules ??
            [
                new HallOfFamePointRuleResponse { Group = "Bowler of the Year Awards", Label = "Bowler of the Year", Points = 12 },
                new HallOfFamePointRuleResponse { Group = "Tournament Titles", Label = "Singles Title", Points = 6 },
                new HallOfFamePointRuleResponse { Group = "Legacy Titles", Label = "NEBA High-Roller Title", Points = 3 }
            ],
            Standings = standings ?? [HallOfFamePointsStandingResponseFactory.Create()],
            RequiredPoints = requiredPoints ?? 36,
            RequiredSeasonsWithPoints = requiredSeasonsWithPoints ?? 3,
            RequiredMembershipYears = requiredMembershipYears ?? 10,
            MembershipDataStartYear = membershipDataStartYear ?? 1999
        };
}