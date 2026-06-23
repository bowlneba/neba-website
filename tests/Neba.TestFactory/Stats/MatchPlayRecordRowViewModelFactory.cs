using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class MatchPlayRecordRowViewModelFactory
{
    public const string ValidBowlerName = "Test Bowler";
    public const int ValidRank = 1;
    public const int ValidWins = 5;
    public const int ValidLoses = 5;
    public const int ValidFinals = 2;
    public const decimal ValidMatchPlayAverage = 200m;
    public const decimal ValidWinnings = 1000m;

    public static MatchPlayRecordRowViewModel Create(
        int? rank = null,
        string? bowlerId = null,
        string? bowlerName = null,
        int? wins = null,
        int? loses = null,
        int? finals = null,
        decimal? matchPlayAverage = null,
        decimal? winnings = null)
        => new()
        {
            Rank = rank ?? ValidRank,
            BowlerId = bowlerId ?? Ulid.NewUlid().ToString(),
            BowlerName = bowlerName ?? ValidBowlerName,
            Wins = wins ?? ValidWins,
            Loses = loses ?? ValidLoses,
            Finals = finals ?? ValidFinals,
            MatchPlayAverage = matchPlayAverage ?? ValidMatchPlayAverage,
            Winnings = winnings ?? ValidWinnings
        };

    public static IReadOnlyCollection<MatchPlayRecordRowViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var rank = 1;
        var totalGames = Math.Max(count + 5, 10);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var currentRank = rank++;
            var loses = currentRank - 1;
            var wins = totalGames - loses;
            return new MatchPlayRecordRowViewModel
            {
                Rank = currentRank,
                BowlerId = Ulid.BogusString(faker),
                BowlerName = faker.Name.FullName(),
                Wins = wins,
                Loses = loses,
                Finals = faker.Random.Int(0, 10),
                MatchPlayAverage = faker.Random.Decimal(150, 250),
                Winnings = faker.Random.Decimal(0, 10000)
            };
        })];
    }

    public static IReadOnlyCollection<MatchPlayRecordRowViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}