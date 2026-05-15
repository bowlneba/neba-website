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

    public static IReadOnlyCollection<FullStatModalRowDto> Bogus(int count, int? seed = null)
    {
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, seed), seed);

        var faker = new Faker<FullStatModalRowDto>()
            .CustomInstantiator(f => new FullStatModalRowDto
            {
                BowlerId = new BowlerId(Ulid.BogusString(f)),
                BowlerName = bowlerNamePool.GetNext(),
                Points = f.Random.Int(0, 1000),
                Average = f.Random.Decimal(150, 230),
                Games = f.Random.Int(20, 200),
                Finals = f.Random.Int(0, 15),
                Wins = f.Random.Int(0, 10),
                Losses = f.Random.Int(0, 10),
                WinPercentage = f.Random.Decimal(0, 100),
                MatchPlayAverage = f.Random.Decimal(150, 230),
                Winnings = f.Random.Decimal(0, 5000),
                FieldAverage = f.Random.Decimal(-10, 15),
                Tournaments = f.Random.Int(1, 15)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}