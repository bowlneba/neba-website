using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.GetSeriesAdvancement;

namespace Neba.TestFactory.Series;

internal static class SurvivorOutcomeDtoFactory
{
    public const string ValidBowlerName = "Dave Smith";
    public const int ValidBlockTotal = 1200;
    public const bool ValidAdvanced = true;

    /// <summary>An advancing bowler with a block total. Pass a null block total for a bowler who did not bowl.</summary>
    public static SurvivorOutcomeDto Create(
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        int? blockTotal = ValidBlockTotal,
        bool? advanced = null)
        => new(bowlerId ?? BowlerId.New(), bowlerName ?? ValidBowlerName, blockTotal, advanced ?? ValidAdvanced);

    internal static IReadOnlyList<SurvivorOutcomeDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            new BowlerId(Ulid.BogusString(faker)),
            faker.Name.FullName(),
            faker.Random.Int(800, 1800),
            faker.Random.Bool()))];
    }

    public static IReadOnlyList<SurvivorOutcomeDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}