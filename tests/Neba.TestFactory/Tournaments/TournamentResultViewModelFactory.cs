using Bogus.DataSets;

using Neba.TestFactory.Bowlers;
using Neba.Website.Server.Tournaments.Detail;

namespace Neba.TestFactory.Tournaments;

public static class TournamentResultViewModelFactory
{
    public const string ValidBowlerName = "Jane Smith";
    public const decimal ValidPrizeMoney = 500m;
    public const int ValidPoints = 10;

    public static TournamentResultViewModel Create(
        string? bowlerName = null,
        int? place = null,
        decimal? prizeMoney = null,
        int? points = null,
        string? sideCutName = null,
        string? sideCutIndicator = null)
        => new()
        {
            BowlerName = bowlerName ?? ValidBowlerName,
            Place = place,
            PrizeMoney = prizeMoney ?? ValidPrizeMoney,
            Points = points ?? ValidPoints,
            SideCutName = sideCutName,
            SideCutIndicator = sideCutIndicator,
        };

    public static IReadOnlyCollection<TournamentResultViewModel> Bogus(int count, int? seed = null)
    {
        var names = UniquePool.Create(NameFactory.Bogus(count, seed), seed);
        var places = UniquePool.CreateNullable(Enumerable.Range(1, count), seed);

        var faker = new Faker<TournamentResultViewModel>()
            .CustomInstantiator(f =>
            {
                var hasSideCut = f.Random.Bool();
                return new TournamentResultViewModel
                {
                    BowlerName = names.GetNext().ToDisplayName(),
                    Place = places.GetNextNullable(),
                    PrizeMoney = f.Random.Decimal(1, 1000),
                    Points = f.Random.Int(1, 100),
                    SideCutName = hasSideCut ? f.Lorem.Word() : null,
                    SideCutIndicator = hasSideCut ? f.Internet.Color(format: ColorFormat.Hex) : null,
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
