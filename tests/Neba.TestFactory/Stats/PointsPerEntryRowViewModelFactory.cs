using Bogus;

using Neba.Domain.Bowlers;
using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class PointsPerEntryRowViewModelFactory
{
    public const string ValidBowlerName = "Test Bowler";
    public const int ValidRank = 1;
    public const int ValidPoints = 100;
    public const int ValidEntries = 10;

    public static PointsPerEntryRowViewModel Create(
        int? rank = null,
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        int? points = null,
        int? entries = null)
        => new()
        {
            Rank = rank ?? ValidRank,
            BowlerId = bowlerId?.Value ?? Ulid.NewUlid(),
            BowlerName = bowlerName ?? ValidBowlerName,
            Entries = entries ?? ValidEntries,
            Points = points ?? ValidPoints
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