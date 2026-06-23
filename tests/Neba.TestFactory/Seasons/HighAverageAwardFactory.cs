using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;

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
        Faker faker,
        UniquePool<BowlerId>? bowlerIds = null)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new HighAverageAward
        {
            Id = new SeasonAwardId(Ulid.BogusString(faker)),
            BowlerId = bowlerIds?.GetNext() ?? new BowlerId(Ulid.BogusString(faker)),
            Average = faker.Random.Decimal(200, 250),
            TotalGames = faker.Random.Int(40, 60),
            TournamentsParticipated = faker.Random.Int(10, 15)
        })];
    }

    public static IReadOnlyCollection<HighAverageAward> Bogus(
        int count,
        UniquePool<BowlerId>? bowlerIds = null,
        int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker, bowlerIds);
    }
}