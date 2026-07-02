using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class SeasonAtAGlanceViewModelFactory
{
    public const int ValidTotalEntries = 50;
    public const decimal ValidTotalPrizeMoney = 5000m;

    public static SeasonAtAGlanceViewModel Create(
        int? totalEntries = null,
        decimal? totalPrizeMoney = null)
        => new()
        {
            TotalEntries = totalEntries ?? ValidTotalEntries,
            TotalPrizeMoney = totalPrizeMoney ?? ValidTotalPrizeMoney,
        };

    internal static IReadOnlyCollection<SeasonAtAGlanceViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new SeasonAtAGlanceViewModel
        {
            TotalEntries = faker.Random.Int(0, 200),
            TotalPrizeMoney = faker.Random.Decimal(0, 10000),
        })];
    }

    public static IReadOnlyCollection<SeasonAtAGlanceViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}