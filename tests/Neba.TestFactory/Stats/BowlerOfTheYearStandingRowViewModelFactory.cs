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

    internal static IReadOnlyCollection<BowlerOfTheYearStandingRowViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var ranking = 1;
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var rank = ranking++;
            return new BowlerOfTheYearStandingRowViewModel
            {
                Rank = rank,
                BowlerId = Ulid.BogusString(faker),
                BowlerName = faker.Name.FullName(),
                Points = Math.Max(0, count - rank + 1),
                Tournaments = faker.Random.Int(0, 20),
                Entries = faker.Random.Int(0, 50),
                Finals = faker.Random.Int(0, 10),
                AverageFinish = faker.Random.Decimal(1, 10),
                Winnings = faker.Random.Decimal(0, 10000)
            };
        })];
    }

    public static IReadOnlyCollection<BowlerOfTheYearStandingRowViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}