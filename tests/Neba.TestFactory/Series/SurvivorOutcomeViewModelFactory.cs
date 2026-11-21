using Neba.Website.Server.Series;

namespace Neba.TestFactory.Series;

public static class SurvivorOutcomeViewModelFactory
{
    public const string ValidBowlerId = "01JBQ5Y7K3M9N2P4R6T8V0W2A1";
    public const string ValidBowlerName = "Dave Smith";
    public const int ValidBlockTotal = 1200;

    /// <summary>A bowler who bowled a full block and advanced.</summary>
    internal static SurvivorOutcomeViewModel Create(
        string? bowlerId = null,
        string? bowlerName = null,
        int? blockTotal = ValidBlockTotal,
        bool advanced = true)
        => new(bowlerId ?? ValidBowlerId, bowlerName ?? ValidBowlerName, blockTotal, advanced);

    /// <summary>Totals fall as the index rises; a bowler advances only with a total, and no total means no full block.</summary>
    internal static IReadOnlyList<SurvivorOutcomeViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(index =>
        {
            int? total = index % 5 == 4 ? null : 1500 - (index * 10) - faker.Random.Int(0, 9);
            return Create(
                bowlerId: Ulid.BogusString(faker),
                bowlerName: faker.Name.FullName(),
                blockTotal: total,
                advanced: total is not null && index % 2 == 0);
        })];
    }
}