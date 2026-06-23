using Neba.Api.Contracts.Awards;
using Neba.Api.Features.Seasons.Domain;

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

    internal static IReadOnlyCollection<BowlerOfTheYearAwardResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new BowlerOfTheYearAwardResponse
        {
            Season = $"{faker.Date.PastDateOnly(100).Year} Season",
            BowlerName = faker.Person.FullName,
            Category = faker.PickRandom(BowlerOfTheYearCategory.List.Select(c => c.Name))
        })];
    }

    public static IReadOnlyCollection<BowlerOfTheYearAwardResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}