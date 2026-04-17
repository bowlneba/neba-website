using Bogus;

using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Bowlers;
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

    public static IReadOnlyCollection<FinalsPerEntryDto> Bogus(int count, int? seed = null)
    {
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, seed), seed);

        var faker = new Faker<FinalsPerEntryDto>()
            .CustomInstantiator(f => new FinalsPerEntryDto
            {
                BowlerId = new BowlerId(Ulid.BogusString(f)),
                BowlerName = bowlerNamePool.GetNext(),
                Finals = f.Random.Int(1, 15),
                Entries = f.Random.Int(1, 20),
                FinalsPerEntry = f.Random.Decimal(0.05m, 1.0m)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
