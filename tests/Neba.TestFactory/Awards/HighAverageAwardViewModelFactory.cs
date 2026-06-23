using Neba.Website.Server.History.Awards;

namespace Neba.TestFactory.Awards;

public static class HighAverageAwardViewModelFactory
{
    public const string ValidSeason = "2025 Season";
    public const string ValidBowlerName = "Joe Bowler";
    public const decimal ValidAverage = 230.55m;
    public const int ValidTotalGames = 45;
    public const int ValidTournamentsParticipated = 10;

    public static HighAverageAwardViewModel Create(
        string? season = null,
        string? bowlerName = null,
        decimal? average = null,
        int? totalGames = null,
        int? tournamentsParticipated = null)
        => new()
        {
            Season = season ?? ValidSeason,
            BowlerName = bowlerName ?? ValidBowlerName,
            Average = average ?? ValidAverage,
            TotalGames = totalGames ?? ValidTotalGames,
            TournamentsParticipated = tournamentsParticipated ?? ValidTournamentsParticipated
        };

    public static IReadOnlyCollection<HighAverageAwardViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new HighAverageAwardViewModel
        {
            Season = $"{faker.Date.PastDateOnly(100).Year} Season",
            BowlerName = faker.Person.FullName,
            Average = faker.Random.Decimal(225, 235),
            TotalGames = faker.Random.Int(70, 100),
            TournamentsParticipated = faker.Random.Int(10, 20)
        })];
    }

    public static IReadOnlyCollection<HighAverageAwardViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}