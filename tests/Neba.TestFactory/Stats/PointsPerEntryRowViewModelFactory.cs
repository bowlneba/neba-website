using Bogus;

using Neba.Domain.Bowlers;
using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class PointsPerEntryRowViewModelFactory
{
    public static PointsPerEntryRowViewModel Create(
        int? rank = null,
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        int? points = null,
        int? entries = null)
        => new()
        {
            Rank = rank ?? 1,
            BowlerId = bowlerId?.Value ?? Ulid.NewUlid(),
            BowlerName = bowlerName ?? "Test Bowler",
            Entries = entries ?? 10,
            Points = points ?? 100
        };

    public static IReadOnlyCollection<PointsPerEntryRowViewModel> Bogus(int count, int? seed = null)
    {
        var rank = 1;
        const int entries = 10;

        var faker = new Faker<PointsPerEntryRowViewModel>()
            .CustomInstantiator(f =>
            {
                var currentRank = rank++;
                var points = Math.Max(0, count - currentRank + 1);

                return new PointsPerEntryRowViewModel
                {
                    Rank = currentRank,
                    BowlerId = Ulid.Bogus(f),
                    BowlerName = f.Name.FullName(),
                    Points = points,
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