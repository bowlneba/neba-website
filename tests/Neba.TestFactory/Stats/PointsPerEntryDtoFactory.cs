using Neba.Api.Features.Stats.GetSeasonStats;
using Neba.Api.Features.Bowlers.Domain;
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

    public static IReadOnlyCollection<PointsPerEntryDto> Bogus(int count, int? seed = null)
    {
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, seed), seed);

        var faker = new Faker<PointsPerEntryDto>()
            .CustomInstantiator(f => new PointsPerEntryDto
            {
                BowlerId = new BowlerId(Ulid.BogusString(f)),
                BowlerName = bowlerNamePool.GetNext(),
                PointsPerEntry = f.Random.Decimal(10, 100),
                Points = f.Random.Int(50, 1000),
                Entries = f.Random.Int(1, 20)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}