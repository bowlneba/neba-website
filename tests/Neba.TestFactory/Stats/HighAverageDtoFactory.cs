using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;
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

    internal static IReadOnlyCollection<HighAverageDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);
        return [.. Enumerable.Range(0, count).Select(_ => new HighAverageDto
        {
            BowlerId = new BowlerId(Ulid.BogusString(faker)),
            BowlerName = bowlerNamePool.GetNext(),
            Average = faker.Random.Decimal(150, 230),
            Games = faker.Random.Int(20, 200),
            Tournaments = faker.Random.Int(1, 15),
            FieldAverage = faker.Random.Decimal(-10, 15)
        })];
    }

    public static IReadOnlyCollection<HighAverageDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}