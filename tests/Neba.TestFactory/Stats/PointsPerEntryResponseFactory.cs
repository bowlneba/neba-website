using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class PointsPerEntryResponseFactory
{
    public const string ValidBowlerId = "01JWXYZTEST000000000000009";
    public const string ValidBowlerName = "Jane Smith";
    public const decimal ValidPointsPerEntry = 26.67m;
    public const int ValidPoints = 320;
    public const int ValidEntries = 12;

    public static PointsPerEntryResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        decimal? pointsPerEntry = null,
        int? points = null,
        int? entries = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            PointsPerEntry = pointsPerEntry ?? ValidPointsPerEntry,
            Points = points ?? ValidPoints,
            Entries = entries ?? ValidEntries
        };

    internal static IReadOnlyCollection<PointsPerEntryResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var points = faker.Random.Int(50, 500);
            var entries = faker.Random.Int(1, 20);
            return new PointsPerEntryResponse
            {
                BowlerId = Ulid.BogusString(faker),
                BowlerName = faker.Name.FullName(),
                PointsPerEntry = Math.Round((decimal)points / entries, 2),
                Points = points,
                Entries = entries
            };
        })];
    }

    public static IReadOnlyCollection<PointsPerEntryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}