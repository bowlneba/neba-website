using Neba.Api.Features.Awards.ListBowlerOfTheYearAwards;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Awards;

public static class BowlerOfTheYearAwardDtoFactory
{
    public const string ValidSeason = "2025 Season";
    public const string ValidCategory = "Open";

    public static BowlerOfTheYearAwardDto Create(
        string? season = null,
        Name? bowlerName = null,
        BowlerOfTheYearCategory? category = null)
        => new()
        {
            Season = season ?? ValidSeason,
            BowlerName = bowlerName ?? NameFactory.Create(),
            Category = category?.Name ?? ValidCategory
        };

    public static IReadOnlyCollection<BowlerOfTheYearAwardDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNames = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);
        var categories = BowlerOfTheYearCategory.List.Select(c => c.Name).ToArray();

        return [.. Enumerable.Range(0, count).Select(_ => new BowlerOfTheYearAwardDto
        {
            Season = $"Season {faker.Date.Past(5).Year}",
            BowlerName = bowlerNames.GetNext(),
            Category = faker.PickRandom(categories)
        })];
    }

    public static IReadOnlyCollection<BowlerOfTheYearAwardDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}