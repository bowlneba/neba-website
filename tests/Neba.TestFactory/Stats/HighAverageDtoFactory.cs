using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Bowlers;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Stats;

public static class HighAverageDtoFactory
{
    public const decimal ValidAverage = 190.50m;
    public const int ValidGames = 120;
    public const int ValidTournaments = 10;
    public const decimal ValidFieldAverage = 5.50m;

    public static HighAverageDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        decimal? average = null,
        int? games = null,
        int? tournaments = null,
        decimal? fieldAverage = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create(),
            Average = average ?? ValidAverage,
            Games = games ?? ValidGames,
            Tournaments = tournaments ?? ValidTournaments,
            FieldAverage = fieldAverage ?? ValidFieldAverage
        };

    public static IReadOnlyCollection<HighAverageDto> Bogus(int count, int? seed = null)
    {
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, seed), seed);

        var faker = new Faker<HighAverageDto>()
            .CustomInstantiator(f => new HighAverageDto
            {
                BowlerId = new BowlerId(Ulid.BogusString(f)),
                BowlerName = bowlerNamePool.GetNext(),
                Average = f.Random.Decimal(150, 230),
                Games = f.Random.Int(20, 200),
                Tournaments = f.Random.Int(1, 15),
                FieldAverage = f.Random.Decimal(-10, 15)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}