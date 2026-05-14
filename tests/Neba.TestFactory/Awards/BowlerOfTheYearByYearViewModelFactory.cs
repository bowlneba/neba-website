using Neba.Website.Server.History.Awards;

namespace Neba.TestFactory.Awards;

public static class BowlerOfTheYearByYearViewModelFactory
{
    public const string ValidSeason = "2025 Season";
    public const string ValidCategory = "Bowler of the Year";
    public const string ValidBowlerName = "Joe Bowler";

    public static BowlerOfTheYearByYearViewModel Create(
        string? season = null,
        IReadOnlyList<KeyValuePair<string, string>>? winnersByCategory = null)
        => new()
        {
            Season = season ?? ValidSeason,
            WinnersByCategory = winnersByCategory ?? [new KeyValuePair<string, string>(ValidCategory, ValidBowlerName)]
        };

    public static IReadOnlyList<BowlerOfTheYearByYearViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BowlerOfTheYearByYearViewModel>()
            .CustomInstantiator(f => new()
            {
                Season = $"{f.Date.PastDateOnly(100).Year} Season",
                WinnersByCategory = [new KeyValuePair<string, string>("Bowler of the Year", f.Person.FullName)]
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}