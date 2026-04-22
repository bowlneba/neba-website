using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class BowlerOfTheYearStandingRowViewModelFactory
{
    public const string ValidBowlerName = "Test Bowler";
    public const int ValidRank = 1;
    public const int ValidPoints = 100;
    public const int ValidTournaments = 5;
    public const int ValidEntries = 10;
    public const int ValidFinals = 2;
    public const decimal ValidAverageFinish = 3.5m;
    public const decimal ValidWinnings = 5000m;

    public static BowlerOfTheYearStandingRowViewModel Create(
        int? rank = null,
        string? bowlerId = null,
        string? bowlerName = null,
        int? points = null,
        int? tournaments = null,
        int? entries = null,
        int? finals = null,
        decimal? averageFinish = null,
        decimal? winnings = null)
        => new()
        {
            Rank = rank ?? ValidRank,
            BowlerId = bowlerId ?? Ulid.NewUlid().ToString(),
            BowlerName = bowlerName ?? ValidBowlerName,
            Points = points ?? ValidPoints,
            Tournaments = tournaments ?? ValidTournaments,
            Entries = entries ?? ValidEntries,
            Finals = finals ?? ValidFinals,
            AverageFinish = averageFinish ?? ValidAverageFinish,
            Winnings = winnings ?? ValidWinnings
        };

    public static IReadOnlyCollection<BowlerOfTheYearStandingRowViewModel> Bogus(int count, int? seed = null)
    {
        var ranking = 1;

        var faker = new Faker<BowlerOfTheYearStandingRowViewModel>()
            .CustomInstantiator(f =>
            {
                var rank = ranking++;

                return new BowlerOfTheYearStandingRowViewModel
                {
                    Rank = rank,
                    BowlerId = Ulid.BogusString(f),
                    BowlerName = f.Name.FullName(),
                    Points = Math.Max(0, count - rank + 1),
                    Tournaments = f.Random.Int(0, 20),
                    Entries = f.Random.Int(0, 50),
                    Finals = f.Random.Int(0, 10),
                    AverageFinish = f.Random.Decimal(1, 10),
                    Winnings = f.Random.Decimal(0, 10000)
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}