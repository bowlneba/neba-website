using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class BowlerOfTheYearStandingResponseFactory
{
    public const string ValidBowlerId = "01JWXYZTEST000000000000003";
    public const string ValidBowlerName = "Jane Smith";
    public const int ValidPoints = 320;
    public const int ValidTournaments = 10;
    public const int ValidEntries = 12;
    public const int ValidFinals = 5;
    public const decimal ValidAverageFinish = 3.2m;
    public const decimal ValidWinnings = 1500m;

    public static BowlerOfTheYearStandingResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        int? points = null,
        int? tournaments = null,
        int? entries = null,
        int? finals = null,
        decimal? averageFinish = ValidAverageFinish,
        decimal? winnings = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            Points = points ?? ValidPoints,
            Tournaments = tournaments ?? ValidTournaments,
            Entries = entries ?? ValidEntries,
            Finals = finals ?? ValidFinals,
            AverageFinish = averageFinish,
            Winnings = winnings ?? ValidWinnings
        };

    internal static IReadOnlyCollection<BowlerOfTheYearStandingResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new BowlerOfTheYearStandingResponse
        {
            BowlerId = Ulid.BogusString(faker),
            BowlerName = faker.Name.FullName(),
            Points = faker.Random.Int(50, 500),
            Tournaments = faker.Random.Int(1, 15),
            Entries = faker.Random.Int(1, 20),
            Finals = faker.Random.Int(0, 10),
            AverageFinish = faker.Random.Bool() ? faker.Random.Decimal(1, 10) : null,
            Winnings = faker.Random.Decimal(0, 5000)
        })];
    }

    public static IReadOnlyCollection<BowlerOfTheYearStandingResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}