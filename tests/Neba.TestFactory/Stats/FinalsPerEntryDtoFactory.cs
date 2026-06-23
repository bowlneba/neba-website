using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Stats;

public static class FinalsPerEntryDtoFactory
{
    public const int ValidFinals = 3;
    public const int ValidEntries = 15;
    public const decimal ValidFinalsPerEntry = 0.20m;

    public static FinalsPerEntryDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        int? finals = null,
        int? entries = null,
        decimal? finalsPerEntry = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create(),
            Finals = finals ?? ValidFinals,
            Entries = entries ?? ValidEntries,
            FinalsPerEntry = finalsPerEntry ?? ValidFinalsPerEntry
        };

    public static IReadOnlyCollection<FinalsPerEntryDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);
        return [.. Enumerable.Range(0, count).Select(_ => new FinalsPerEntryDto
        {
            BowlerId = new BowlerId(Ulid.BogusString(faker)),
            BowlerName = bowlerNamePool.GetNext(),
            Finals = faker.Random.Int(1, 15),
            Entries = faker.Random.Int(1, 20),
            FinalsPerEntry = faker.Random.Decimal(0.05m, 1.0m)
        })];
    }

    public static IReadOnlyCollection<FinalsPerEntryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}