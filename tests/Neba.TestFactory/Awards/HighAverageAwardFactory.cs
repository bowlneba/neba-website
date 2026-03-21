using Bogus;

using Neba.Domain.Awards;
using Neba.Domain.Bowlers;

namespace Neba.TestFactory.Awards;

public static class HighAverageAwardFactory
{
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
            Average = average ?? 220.5m,
            TotalGames = totalGames ?? 50,
            TournamentsParticipated = tournamentsParticipated ?? 12
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
                Id = new SeasonAwardId(Ulid.Bogus(f)),
                BowlerId = bowlerIds?.GetNext() ?? new BowlerId(Ulid.Bogus(f)),
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