using Bogus;

using Neba.Domain.Bowlers;
using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class MatchPlayAppearancesRowViewModelFactory
{
    public static MatchPlayAppearancesRowViewModel Create(
        int? rank = null,
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        int? finals = null,
        int? tournaments = null,
        int? entries = null)
        => new()
        {
            Rank = rank ?? 1,
            BowlerId = bowlerId?.Value ?? Ulid.NewUlid(),
            BowlerName = bowlerName ?? "Test Bowler",
            Finals = finals ?? 5,
            Tournaments = tournaments ?? 6,
            Entries = entries ?? 8
        };

    public static IReadOnlyCollection<MatchPlayAppearancesRowViewModel> Bogus(int count, int? seed = null)
    {
        var rank = 1;

        var faker = new Faker<MatchPlayAppearancesRowViewModel>()
            .CustomInstantiator(f =>
            {
                var currentRank = rank++;
                var finals = Math.Max(0, count - currentRank + 1);
                var tournaments = finals + f.Random.Int(0, 5);
                var entries = tournaments + f.Random.Int(0, 8);

                return new MatchPlayAppearancesRowViewModel
                {
                    Rank = currentRank,
                    BowlerId = Ulid.Bogus(f),
                    BowlerName = f.Name.FullName(),
                    Finals = finals,
                    Tournaments = tournaments,
                    Entries = entries
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}