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

    internal static IReadOnlyList<BowlerOfTheYearByYearViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new BowlerOfTheYearByYearViewModel
        {
            Season = $"{faker.Date.PastDateOnly(100).Year} Season",
            WinnersByCategory = [new KeyValuePair<string, string>("Bowler of the Year", faker.Person.FullName)]
        })];
    }

    public static IReadOnlyList<BowlerOfTheYearByYearViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}