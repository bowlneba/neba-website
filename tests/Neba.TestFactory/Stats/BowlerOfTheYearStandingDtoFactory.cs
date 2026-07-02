using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Stats;

public static class BowlerOfTheYearStandingDtoFactory
{
    public const int ValidPoints = 500;
    public const int ValidTournaments = 10;
    public const int ValidEntries = 15;
    public const int ValidFinals = 3;
    public const decimal ValidAverageFinish = 4.5m;
    public const decimal ValidWinnings = 2500m;

    public static BowlerOfTheYearStandingDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        int? points = null,
        int? tournaments = null,
        int? entries = null,
        int? finals = null,
        decimal? averageFinish = null,
        decimal? winnings = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create(),
            Points = points ?? ValidPoints,
            Tournaments = tournaments ?? ValidTournaments,
            Entries = entries ?? ValidEntries,
            Finals = finals ?? ValidFinals,
            AverageFinish = averageFinish ?? ValidAverageFinish,
            Winnings = winnings ?? ValidWinnings
        };

    internal static IReadOnlyCollection<BowlerOfTheYearStandingDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);
        return [.. Enumerable.Range(0, count).Select(_ => new BowlerOfTheYearStandingDto
        {
            BowlerId = new BowlerId(Ulid.BogusString(faker)),
            BowlerName = bowlerNamePool.GetNext(),
            Points = faker.Random.Int(50, 1000),
            Tournaments = faker.Random.Int(1, 15),
            Entries = faker.Random.Int(1, 20),
            Finals = faker.Random.Int(0, 10),
            AverageFinish = faker.Random.Bool() ? faker.Random.Decimal(1, 10) : null,
            Winnings = faker.Random.Decimal(0, 5000)
        })];
    }

    public static IReadOnlyCollection<BowlerOfTheYearStandingDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}