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
        string? bowlerId = null,
        string? bowlerName = null,
        int? points = null,
        int? entries = null)
        => new()
        {
            Rank = rank ?? ValidRank,
            BowlerId = bowlerId ?? Ulid.NewUlid().ToString(),
            BowlerName = bowlerName ?? ValidBowlerName,
            Entries = entries ?? ValidEntries,
            Points = points ?? ValidPoints
        };

    internal static IReadOnlyCollection<PointsPerEntryRowViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var rank = 1;
        const int entries = 10;
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var currentRank = rank++;
            var points = Math.Max(0, count - currentRank + 1);
            return new PointsPerEntryRowViewModel
            {
                Rank = currentRank,
                BowlerId = Ulid.BogusString(faker),
                BowlerName = faker.Name.FullName(),
                Points = points,
                Entries = entries
            };
        })];
    }

    public static IReadOnlyCollection<PointsPerEntryRowViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}