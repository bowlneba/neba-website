using Neba.Api.Contracts.Series.GetSeriesAdvancers;

namespace Neba.TestFactory.Series;

public static class AdvancerResponseFactory
{
    public const string ValidBowlerId = "01JBQ5Y7K3M9N2P4R6T8V0W3A1";
    public const string ValidBowlerName = "Dave Smith";
    public const int ValidPlace = 1;

    /// <summary>A qualifier in first place. Pass a null place for a Backfill entry, or a block total for a Survivor Stage.</summary>
    public static AdvancerResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        int? place = ValidPlace,
        int? blockTotal = null,
        bool? isBackfill = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            Place = place,
            BlockTotal = blockTotal,
            IsBackfill = isBackfill ?? false,
        };

    internal static IReadOnlyList<AdvancerResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(index => Create(
            Ulid.BogusString(faker),
            faker.Name.FullName(),
            index + 1))];
    }

    public static IReadOnlyList<AdvancerResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
