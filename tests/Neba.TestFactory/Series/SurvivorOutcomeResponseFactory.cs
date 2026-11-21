using Neba.Api.Contracts.Series.GetSeriesAdvancement;

namespace Neba.TestFactory.Series;

public static class SurvivorOutcomeResponseFactory
{
    public const string ValidBowlerId = "01JBQ5Y7K3M9N2P4R6T8V0W2A1";
    public const string ValidBowlerName = "Dave Smith";
    public const int ValidBlockTotal = 1200;
    public const bool ValidAdvanced = true;

    /// <summary>An advancing bowler with a block total. Pass a null block total for a bowler who did not bowl.</summary>
    public static SurvivorOutcomeResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        int? blockTotal = ValidBlockTotal,
        bool? advanced = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            BlockTotal = blockTotal,
            Advanced = advanced ?? ValidAdvanced,
        };

    internal static IReadOnlyList<SurvivorOutcomeResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            Ulid.BogusString(faker),
            faker.Name.FullName(),
            faker.Random.Int(800, 1800),
            faker.Random.Bool()))];
    }

    public static IReadOnlyList<SurvivorOutcomeResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}