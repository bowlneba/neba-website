using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Stats;

public static class FullStatModalRowDtoFactory
{
    public const int ValidPoints = 500;
    public const decimal ValidAverage = 190.00m;
    public const int ValidGames = 120;
    public const int ValidFinals = 3;
    public const int ValidWins = 4;
    public const int ValidLosses = 2;
    public const decimal ValidWinPercentage = 66.67m;
    public const decimal ValidMatchPlayAverage = 200.00m;
    public const decimal ValidWinnings = 2500m;
    public const decimal ValidFieldAverage = 5.50m;
    public const int ValidTournaments = 10;

    public static FullStatModalRowDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        int? points = null,
        decimal? average = null,
        int? games = null,
        int? finals = null,
        int? wins = null,
        int? losses = null,
        decimal? winPercentage = null,
        decimal? matchPlayAverage = null,
        decimal? winnings = null,
        decimal? fieldAverage = null,
        int? tournaments = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create(),
            Points = points ?? ValidPoints,
            Average = average ?? ValidAverage,
            Games = games ?? ValidGames,
            Finals = finals ?? ValidFinals,
            Wins = wins ?? ValidWins,
            Losses = losses ?? ValidLosses,
            WinPercentage = winPercentage ?? ValidWinPercentage,
            MatchPlayAverage = matchPlayAverage ?? ValidMatchPlayAverage,
            Winnings = winnings ?? ValidWinnings,
            FieldAverage = fieldAverage ?? ValidFieldAverage,
            Tournaments = tournaments ?? ValidTournaments
        };

    internal static IReadOnlyCollection<FullStatModalRowDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);
        return [.. Enumerable.Range(0, count).Select(_ => new FullStatModalRowDto
        {
            BowlerId = new BowlerId(Ulid.BogusString(faker)),
            BowlerName = bowlerNamePool.GetNext(),
            Points = faker.Random.Int(0, 1000),
            Average = faker.Random.Decimal(150, 230),
            Games = faker.Random.Int(20, 200),
            Finals = faker.Random.Int(0, 15),
            Wins = faker.Random.Int(0, 10),
            Losses = faker.Random.Int(0, 10),
            WinPercentage = faker.Random.Decimal(0, 100),
            MatchPlayAverage = faker.Random.Decimal(150, 230),
            Winnings = faker.Random.Decimal(0, 5000),
            FieldAverage = faker.Random.Decimal(-10, 15),
            Tournaments = faker.Random.Int(1, 15)
        })];
    }

    public static IReadOnlyCollection<FullStatModalRowDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}