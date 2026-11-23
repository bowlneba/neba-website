using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;

namespace Neba.TestFactory.Series;

public static class SurvivorOutcomeFactory
{
    public const int ValidBlockTotal = 1200;

    /// <summary>An advancing bowler with a block total.</summary>
    public static SurvivorOutcome Create(
        BowlerId? bowlerId = null,
        int? blockTotal = ValidBlockTotal,
        bool advanced = true)
    {
        var result = SurvivorOutcome.Create(bowlerId ?? BowlerId.New(), blockTotal, advanced);

        return result.IsError
            ? throw new InvalidOperationException($"Failed to create survivor outcome: {result.Errors[0].Description}")
            : result.Value;
    }

    public static IReadOnlyCollection<SurvivorOutcome> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            new BowlerId(Ulid.BogusString(faker)),
            faker.Random.Int(800, 1800),
            faker.Random.Bool()))];
    }
}