using Bogus;

using Neba.Domain.Bowlers;
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
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        int? highBlock = null,
        int? highGame = null)
        => new()
        {
            Rank = rank ?? ValidRank,
            BowlerId = bowlerId?.Value ?? Ulid.NewUlid(),
            BowlerName = bowlerName ?? ValidBowlerName,
            HighBlock = highBlock ?? ValidHighBlock,
            HighGame = highGame ?? ValidHighGame
        };

    public static IReadOnlyCollection<HighBlockRowViewModel> Bogus(int count, int? seed = null)
    {
        var rank = 1;
        const int maxHighBlock = 1400;
        const int minHighBlock = 1250;

        var step = count > 1
            ? (maxHighBlock - minHighBlock) / (count - 1)
            : 0;

        var faker = new Faker<HighBlockRowViewModel>()
            .CustomInstantiator(f =>
            {
                var currentRank = rank++;

                return new HighBlockRowViewModel
                {
                    Rank = currentRank,
                    BowlerId = Ulid.Bogus(f),
                    BowlerName = f.Name.FullName(),
                    HighBlock = maxHighBlock - ((currentRank - 1) * step),
                    HighGame = f.Random.Int(200, 300)
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}