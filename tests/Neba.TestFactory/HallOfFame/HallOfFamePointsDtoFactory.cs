using Neba.Api.Features.HallOfFame.ListHallOfFamePoints;

namespace Neba.TestFactory.HallOfFame;

public static class HallOfFamePointsDtoFactory
{
    public static HallOfFamePointsDto Create(
        IReadOnlyCollection<HallOfFamePointRuleDto>? rules = null,
        IReadOnlyCollection<HallOfFamePointsStandingDto>? standings = null,
        int? requiredPoints = null,
        int? requiredSeasonsWithPoints = null,
        int? requiredMembershipYears = null,
        int? membershipDataStartYear = null)
        => new()
        {
            Rules = rules ??
            [
                new HallOfFamePointRuleDto { Group = "Bowler of the Year Awards", Label = "Bowler of the Year", Points = 12 },
                new HallOfFamePointRuleDto { Group = "Tournament Titles", Label = "Singles Title", Points = 6 }
            ],
            Standings = standings ?? [HallOfFamePointsStandingDtoFactory.Create()],
            RequiredPoints = requiredPoints ?? 36,
            RequiredSeasonsWithPoints = requiredSeasonsWithPoints ?? 3,
            RequiredMembershipYears = requiredMembershipYears ?? 10,
            MembershipDataStartYear = membershipDataStartYear ?? 1999
        };

    public static IReadOnlyCollection<HallOfFamePointsDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            standings: HallOfFamePointsStandingDtoFactory.Bogus(faker.Random.Int(1, 5), faker)))];
    }
}
