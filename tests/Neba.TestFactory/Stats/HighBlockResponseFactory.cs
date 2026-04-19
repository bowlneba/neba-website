using Bogus;

using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class HighBlockResponseFactory
{
    public const string ValidBowlerId = "01JWXYZTEST000000000000005";
    public const string ValidBowlerName = "Jane Smith";
    public const int ValidHighBlock = 1182;
    public const int ValidHighGame = 258;

    public static HighBlockResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        int? highBlock = null,
        int? highGame = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            HighBlock = highBlock ?? ValidHighBlock,
            HighGame = highGame ?? ValidHighGame
        };

    public static IReadOnlyCollection<HighBlockResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<HighBlockResponse>()
            .CustomInstantiator(f => new HighBlockResponse
            {
                BowlerId = Ulid.BogusString(f),
                BowlerName = f.Name.FullName(),
                HighBlock = f.Random.Int(1000, 1300),
                HighGame = f.Random.Int(200, 300)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}