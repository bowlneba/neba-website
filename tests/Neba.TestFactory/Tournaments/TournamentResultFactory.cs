using Bogus;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Tournaments;

public static class TournamentResultFactory
{
    public const int ValidPlace = 1;
    public const decimal ValidPrizeMoney = 100m;
    public const int ValidPoints = 10;

    public static TournamentResult Create(
        BowlerId? bowlerId = null,
        int? place = null,
        decimal? prizeMoney = null,
        int? points = null)
    {
        var result = TournamentResult.Create(
            bowlerId: bowlerId ?? BowlerId.New(),
            place: place ?? ValidPlace,
            prizeMoney: prizeMoney ?? ValidPrizeMoney,
            points: points ?? ValidPoints);

        return result.IsError
            ? throw new InvalidOperationException($"Failed to create tournament result: {result.Errors[0].Description}")
            : result.Value;
    }

    internal static IReadOnlyCollection<TournamentResult> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(i =>
        {
            var result = TournamentResult.Create(
                bowlerId: new BowlerId(Ulid.BogusString(faker)),
                place: i + 1,
                prizeMoney: faker.Random.Decimal(0, 10000),
                points: faker.Random.Int(0, 500));

            return result.IsError
                ? throw new InvalidOperationException($"Failed to create tournament result: {result.Errors[0].Description}")
                : result.Value;
        })];
    }

    public static IReadOnlyCollection<TournamentResult> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
