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

    public static IReadOnlyCollection<HighAverageAwardViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<HighAverageAwardViewModel>()
            .CustomInstantiator(f => new()
            {
                Season = $"{f.Date.PastDateOnly(100).Year} Season",
                BowlerName = f.Person.FullName,
                Average = f.Random.Decimal(225, 235),
                TotalGames = f.Random.Int(70, 100),
                TournamentsParticipated = f.Random.Int(10, 20)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}