using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;

namespace Neba.TestFactory.Series;

public static class SquadBowlerTotalFactory
{
    public const int ValidTotal = 1800;
    public const int ValidGames = 8;

    public static SquadBowlerTotal Create(BowlerId? bowlerId = null, int? total = null, int? games = null)
        => new(bowlerId ?? BowlerId.New(), total ?? ValidTotal, games ?? ValidGames);

    public static IReadOnlyList<SquadBowlerTotal> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            new BowlerId(Ulid.BogusString(faker)),
            faker.Random.Int(1200, 2200),
            faker.Random.Int(1, ValidGames)))];
    }
}