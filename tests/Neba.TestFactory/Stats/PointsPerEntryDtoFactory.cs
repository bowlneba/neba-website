using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Stats;

public static class PointsPerEntryDtoFactory
{
    public const decimal ValidPointsPerEntry = 33.33m;
    public const int ValidPoints = 500;
    public const int ValidEntries = 15;

    public static PointsPerEntryDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        decimal? pointsPerEntry = null,
        int? points = null,
        int? entries = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create(),
            PointsPerEntry = pointsPerEntry ?? ValidPointsPerEntry,
            Points = points ?? ValidPoints,
            Entries = entries ?? ValidEntries
        };

    internal static IReadOnlyCollection<PointsPerEntryDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);
        return [.. Enumerable.Range(0, count).Select(_ => new PointsPerEntryDto
        {
            BowlerId = new BowlerId(Ulid.BogusString(faker)),
            BowlerName = bowlerNamePool.GetNext(),
            PointsPerEntry = faker.Random.Decimal(10, 100),
            Points = faker.Random.Int(50, 1000),
            Entries = faker.Random.Int(1, 20)
        })];
    }

    public static IReadOnlyCollection<PointsPerEntryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}