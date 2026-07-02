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

    internal static IReadOnlyCollection<HighBlockResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new HighBlockResponse
        {
            BowlerId = Ulid.BogusString(faker),
            BowlerName = faker.Name.FullName(),
            HighBlock = faker.Random.Int(1000, 1300),
            HighGame = faker.Random.Int(200, 300)
        })];
    }

    public static IReadOnlyCollection<HighBlockResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}