using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.HallOfFame.ListHallOfFameInductions;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.HallOfFame;

public static class HallOfFameInductionDtoFactory
{
    public const int ValidYear = 2025;

    public static HallOfFameInductionDto Create(
        int? year = null,
        Name? bowlerName = null,
        IReadOnlyCollection<HallOfFameCategory>? categories = null,
        Uri? photoUri = null)
        => new()
        {
            Year = year ?? ValidYear,
            BowlerName = bowlerName ?? NameFactory.Create(),
            Categories = categories ?? [HallOfFameCategory.SuperiorPerformance],
            PhotoUri = photoUri
        };

    public static IReadOnlyCollection<HallOfFameInductionDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNames = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var hasPhoto = faker.Random.Bool();
            return new HallOfFameInductionDto
            {
                Year = faker.Date.PastDateOnly().Year,
                BowlerName = bowlerNames.GetNext(),
                Categories = [.. faker.PickRandom(HallOfFameCategory.List, faker.Random.Int(1, HallOfFameCategory.List.Count))],
                PhotoUri = hasPhoto ? new Uri(faker.Internet.Url()) : null
            };
        })];
    }

    public static IReadOnlyCollection<HallOfFameInductionDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}