using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;

namespace Neba.TestFactory.Seasons;

public static class HighAverageAwardFactory
{
    public const decimal ValidAverage = 220.5m;
    public const int ValidTotalGames = 50;
    public const int ValidTournamentsParticipated = 12;

    public static HighAverageAward Create(
        SeasonAwardId? id = null,
        BowlerId? bowlerId = null,
        decimal? average = null,
        int? totalGames = null,
        int? tournamentsParticipated = null)
    {
        return new HighAverageAward
        {
            Id = id ?? SeasonAwardId.New(),
            BowlerId = bowlerId ?? BowlerId.New(),
            Average = average ?? ValidAverage,
            TotalGames = totalGames ?? ValidTotalGames,
            TournamentsParticipated = tournamentsParticipated ?? ValidTournamentsParticipated
        };
    }

    public static IReadOnlyCollection<HighAverageAward> Bogus(
        int count,
        UniquePool<BowlerId>? bowlerIds = null,
        int? seed = null
    )
    {
        var faker = new Faker<HighAverageAward>()
            .CustomInstantiator(f => new()
            {
                Id = new SeasonAwardId(Ulid.BogusString(f)),
                BowlerId = bowlerIds?.GetNext() ?? new BowlerId(Ulid.BogusString(f)),
                Average = f.Random.Decimal(200, 250),
                TotalGames = f.Random.Int(40, 60),
                TournamentsParticipated = f.Random.Int(10, 15)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}