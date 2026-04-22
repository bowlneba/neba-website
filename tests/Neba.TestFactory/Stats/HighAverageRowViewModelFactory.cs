using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class HighAverageRowViewModelFactory
{
    public const string ValidBowlerName = "Test Bowler";
    public const int ValidRank = 1;
    public const decimal ValidAverage = 200m;
    public const int ValidGames = 10;
    public const int ValidTournaments = 5;
    public const decimal ValidFieldAverage = 10.2m;

    public static HighAverageRowViewModel Create(
        int? rank = null,
        string? bowlerId = null,
        string? bowlerName = null,
        decimal? average = null,
        int? games = null,
        int? tournaments = null,
        decimal? fieldAverage = null)
        => new()
        {
            Rank = rank ?? ValidRank,
            BowlerId = bowlerId ?? Ulid.NewUlid().ToString(),
            BowlerName = bowlerName ?? ValidBowlerName,
            Average = average ?? ValidAverage,
            Games = games ?? ValidGames,
            Tournaments = tournaments ?? ValidTournaments,
            FieldAverage = fieldAverage ?? ValidFieldAverage
        };

    public static IReadOnlyCollection<HighAverageRowViewModel> Bogus(int count, int? seed = null)
    {
        var rank = 1;
        const decimal maxAverage = 250m;
        const decimal minAverage = 150m;
        var step = count > 1
            ? (maxAverage - minAverage) / (count - 1)
            : 0m;

        var faker = new Faker<HighAverageRowViewModel>()
            .CustomInstantiator(f =>
            {
                var currentRank = rank++;

                return new HighAverageRowViewModel
                {
                    Rank = currentRank,
                    BowlerId = Ulid.BogusString(f),
                    BowlerName = f.Name.FullName(),
                    Average = decimal.Round(maxAverage - ((currentRank - 1) * step), 3),
                    Games = f.Random.Int(0, 20),
                    Tournaments = f.Random.Int(0, 10),
                    FieldAverage = f.Random.Decimal(150, 250)
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}