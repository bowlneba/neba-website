using Bogus;

using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class BowlerOfTheYearStandingRowViewModelFactory
{
    public static BowlerOfTheYearStandingRowViewModel Create(
        int? rank = null,
        Ulid? bowlerId = null,
        string? bowlerName = null,
        int? points = null,
        int? tournaments = null,
        int? entries = null,
        int? finals = null,
        decimal? averageFinish = null,
        decimal? winnings = null)
        => new()
        {
            Rank = rank ?? 1,
            BowlerId = bowlerId ?? Ulid.NewUlid(),
            BowlerName = bowlerName ?? "Test Bowler",
            Points = points ?? 100,
            Tournaments = tournaments ?? 5,
            Entries = entries ?? 10,
            Finals = finals ?? 2,
            AverageFinish = averageFinish ?? 3.5m,
            Winnings = winnings ?? 5000m
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
                    BowlerId = Ulid.NewUlid(),
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