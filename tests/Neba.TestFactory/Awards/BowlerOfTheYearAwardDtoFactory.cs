using Neba.Application.Awards.ListBowlerOfTheYearAwards;
using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;
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

    public static IReadOnlyCollection<BowlerOfTheYearAwardDto> Bogus(int count, int? seed = null)
    {
        var bowlerNames = UniquePool.Create(NameFactory.Bogus(count, seed), seed);
        var categories = BowlerOfTheYearCategory.List.Select(c => c.Name).ToArray();

        var faker = new Bogus.Faker<BowlerOfTheYearAwardDto>()
            .CustomInstantiator(f => new()
            {
                Season = $"Season {f.Date.Past(5).Year}",
                BowlerName = bowlerNames.GetNext(),
                Category = f.PickRandom(categories)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}