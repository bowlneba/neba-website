using Bogus;

using Neba.Api.Contracts.Awards;
using Neba.Domain.Seasons;

namespace Neba.TestFactory.Awards;

public static class BowlerOfTheYearAwardResponseFactory
{
    public const string ValidSeason = "2025 Season";
    public const string ValidBowlerName = "Joe Bowler";
    public static string ValidCategory
        => BowlerOfTheYearCategory.Open.Name;

    public static BowlerOfTheYearAwardResponse Create(
        string? season = null,
        string? bowlerName = null,
        string? category = null)
        => new()
        {
            Season = season ?? ValidSeason,
            BowlerName = bowlerName ?? ValidBowlerName,
            Category = category ?? ValidCategory
        };

    public static IReadOnlyCollection<BowlerOfTheYearAwardResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BowlerOfTheYearAwardResponse>()
            .CustomInstantiator(f => new()
            {
                Season = $"{f.Date.PastDateOnly(100).Year} Season",
                BowlerName = f.Person.FullName,
                Category = f.PickRandom(BowlerOfTheYearCategory.List.Select(c => c.Name))
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}