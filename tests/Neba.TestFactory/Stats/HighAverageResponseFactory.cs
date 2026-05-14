using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class HighAverageResponseFactory
{
    public const string ValidBowlerId = "01JWXYZTEST000000000000004";
    public const string ValidBowlerName = "Jane Smith";
    public const decimal ValidAverage = 221.4m;
    public const int ValidGames = 60;
    public const int ValidTournaments = 10;
    public const decimal ValidFieldAverage = 12.5m;

    public static HighAverageResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        decimal? average = null,
        int? games = null,
        int? tournaments = null,
        decimal? fieldAverage = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            Average = average ?? ValidAverage,
            Games = games ?? ValidGames,
            Tournaments = tournaments ?? ValidTournaments,
            FieldAverage = fieldAverage ?? ValidFieldAverage
        };

    public static IReadOnlyCollection<HighAverageResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<HighAverageResponse>()
            .CustomInstantiator(f =>
            {
                var games = f.Random.Int(10, 100);
                return new HighAverageResponse
                {
                    BowlerId = Ulid.BogusString(f),
                    BowlerName = f.Name.FullName(),
                    Average = f.Random.Decimal(180, 240),
                    Games = games,
                    Tournaments = f.Random.Int(1, 15),
                    FieldAverage = f.Random.Decimal(-20, 40)
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}