using Bogus;

using Neba.Domain.Bowlers;
using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class HighBlockRowViewModelFactory
{
    public static HighBlockRowViewModel Create(
        int? rank = null,
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        int? highBlock = null,
        int? highGame = null)
        => new()
        {
            Rank = rank ?? 1,
            BowlerId = bowlerId?.Value ?? Ulid.NewUlid(),
            BowlerName = bowlerName ?? "Test Bowler",
            HighBlock = highBlock ?? 600,
            HighGame = highGame ?? 300
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