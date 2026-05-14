using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class FullStatModalRowResponseFactory
{
    public const string ValidBowlerId = "01JWXYZTEST000000000000014";
    public const string ValidBowlerName = "Jane Smith";
    public const int ValidPoints = 320;
    public const decimal ValidAverage = 221.4m;
    public const int ValidGames = 60;
    public const int ValidFinals = 5;
    public const int ValidWins = 9;
    public const int ValidLosses = 6;
    public const decimal ValidWinPercentage = 60.0m;
    public const decimal ValidMatchPlayAverage = 218.0m;
    public const decimal ValidWinnings = 1500m;
    public const decimal ValidFieldAverage = 12.5m;
    public const int ValidTournaments = 10;

    public static FullStatModalRowResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
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
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
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

    public static IReadOnlyCollection<FullStatModalRowResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<FullStatModalRowResponse>()
            .CustomInstantiator(f =>
            {
                var wins = f.Random.Int(1, 20);
                var losses = f.Random.Int(1, 20);
                return new FullStatModalRowResponse
                {
                    BowlerId = Ulid.BogusString(f),
                    BowlerName = f.Name.FullName(),
                    Points = f.Random.Int(20, 500),
                    Average = f.Random.Decimal(180, 240),
                    Games = f.Random.Int(10, 100),
                    Finals = f.Random.Int(0, 10),
                    Wins = wins,
                    Losses = losses,
                    WinPercentage = Math.Round((decimal)wins / (wins + losses) * 100, 2),
                    MatchPlayAverage = f.Random.Decimal(180, 240),
                    Winnings = f.Random.Decimal(0, 5000),
                    FieldAverage = f.Random.Decimal(-20, 40),
                    Tournaments = f.Random.Int(1, 15)
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}