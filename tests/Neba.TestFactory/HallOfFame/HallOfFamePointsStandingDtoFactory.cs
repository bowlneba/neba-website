using Neba.Api.Features.HallOfFame.ListHallOfFamePoints;

namespace Neba.TestFactory.HallOfFame;

public static class HallOfFamePointsStandingDtoFactory
{
    public const string ValidBowlerId = "01JZ8K3M4N5P6Q7R8S9T0V1W3Z";
    public const string ValidBowlerName = "Jane Doe";

    public static HallOfFamePointsStandingDto Create(
        string? bowlerId = null,
        string? bowlerName = null,
        int? totalPoints = null,
        int? seasonsWithPoints = null,
        int? membershipYears = null,
        bool? membershipHistoryComplete = null,
        bool? eligible = null,
        bool? meetsPoints = null,
        bool? meetsSeasons = null,
        IReadOnlyCollection<string>? inductedCategories = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            TotalPoints = totalPoints ?? 18,
            SeasonsWithPoints = seasonsWithPoints ?? 3,
            MembershipYears = membershipYears ?? 11,
            MembershipHistoryComplete = membershipHistoryComplete ?? true,
            Eligible = eligible ?? false,
            MeetsPoints = meetsPoints ?? false,
            MeetsSeasons = meetsSeasons ?? true,
            InductedCategories = inductedCategories ?? []
        };

    internal static IReadOnlyCollection<HallOfFamePointsStandingDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var points = faker.Random.Int(1, 60);
            var seasons = faker.Random.Int(1, 8);
            var years = faker.Random.Int(0, 30);
            var meetsPoints = points >= 36;
            var meetsSeasons = seasons >= 3;

            return new HallOfFamePointsStandingDto
            {
                BowlerId = Ulid.BogusString(faker),
                BowlerName = faker.Name.FullName(),
                TotalPoints = points,
                SeasonsWithPoints = seasons,
                MembershipYears = years,
                MembershipHistoryComplete = faker.Random.Bool(),
                Eligible = meetsPoints && meetsSeasons && years >= 10,
                MeetsPoints = meetsPoints,
                MeetsSeasons = meetsSeasons,
                InductedCategories = []
            };
        })];
    }

    public static IReadOnlyCollection<HallOfFamePointsStandingDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}