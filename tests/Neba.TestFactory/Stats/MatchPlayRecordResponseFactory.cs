using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class MatchPlayRecordResponseFactory
{
    public const string ValidBowlerId = "01JWXYZTEST000000000000007";
    public const string ValidBowlerName = "Jane Smith";
    public const int ValidWins = 9;
    public const int ValidLosses = 6;
    public const decimal ValidWinPercentage = 60.0m;
    public const int ValidFinals = 5;
    public const decimal ValidMatchPlayAverage = 218.0m;
    public const decimal ValidWinnings = 1500m;

    public static MatchPlayRecordResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        int? wins = null,
        int? losses = null,
        decimal? winPercentage = null,
        int? finals = null,
        decimal? matchPlayAverage = null,
        decimal? winnings = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            Wins = wins ?? ValidWins,
            Losses = losses ?? ValidLosses,
            WinPercentage = winPercentage ?? ValidWinPercentage,
            Finals = finals ?? ValidFinals,
            MatchPlayAverage = matchPlayAverage ?? ValidMatchPlayAverage,
            Winnings = winnings ?? ValidWinnings
        };

    public static IReadOnlyCollection<MatchPlayRecordResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<MatchPlayRecordResponse>()
            .CustomInstantiator(f =>
            {
                var wins = f.Random.Int(1, 20);
                var losses = f.Random.Int(1, 20);
                return new MatchPlayRecordResponse
                {
                    BowlerId = Ulid.BogusString(f),
                    BowlerName = f.Name.FullName(),
                    Wins = wins,
                    Losses = losses,
                    WinPercentage = Math.Round((decimal)wins / (wins + losses) * 100, 2),
                    Finals = f.Random.Int(1, 10),
                    MatchPlayAverage = f.Random.Decimal(180, 240),
                    Winnings = f.Random.Decimal(0, 5000)
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}