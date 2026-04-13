using Bogus;

using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class SeasonAtAGlanceViewModelFactory
{
    public const decimal ValidFieldAverage = 185.5m;
    public const int ValidTotalEntries = 50;
    public const decimal ValidTotalPrizeMoney = 5000m;

    public static SeasonAtAGlanceViewModel Create(
        decimal? fieldAverage = null,
        int? totalEntries = null,
        decimal? totalPrizeMoney = null)
        => new()
        {
            FieldAverage = fieldAverage ?? ValidFieldAverage,
            TotalEntries = totalEntries ?? ValidTotalEntries,
            TotalPrizeMoney = totalPrizeMoney ?? ValidTotalPrizeMoney,
        };

    public static IReadOnlyCollection<SeasonAtAGlanceViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SeasonAtAGlanceViewModel>()
            .CustomInstantiator(f => new SeasonAtAGlanceViewModel
            {
                FieldAverage = f.Random.Decimal(150, 250),
                TotalEntries = f.Random.Int(0, 200),
                TotalPrizeMoney = f.Random.Decimal(0, 10000),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
