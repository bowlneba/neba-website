using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class HighBlockRowViewModelFactory
{
    public const string ValidBowlerName = "Test Bowler";
    public const int ValidRank = 1;
    public const int ValidHighBlock = 600;
    public const int ValidHighGame = 300;

    public static HighBlockRowViewModel Create(
        int? rank = null,
        string? bowlerId = null,
        string? bowlerName = null,
        int? highBlock = null,
        int? highGame = null)
        => new()
        {
            Rank = rank ?? ValidRank,
            BowlerId = bowlerId ?? Ulid.NewUlid().ToString(),
            BowlerName = bowlerName ?? ValidBowlerName,
            HighBlock = highBlock ?? ValidHighBlock,
            HighGame = highGame ?? ValidHighGame
        };

    internal static IReadOnlyCollection<HighBlockRowViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var rank = 1;
        const int maxHighBlock = 1400;
        const int minHighBlock = 1250;
        var step = count > 1
            ? (maxHighBlock - minHighBlock) / (count - 1)
            : 0;
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var currentRank = rank++;
            return new HighBlockRowViewModel
            {
                Rank = currentRank,
                BowlerId = Ulid.BogusString(faker),
                BowlerName = faker.Name.FullName(),
                HighBlock = maxHighBlock - ((currentRank - 1) * step),
                HighGame = faker.Random.Int(200, 300)
            };
        })];
    }

    public static IReadOnlyCollection<HighBlockRowViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}