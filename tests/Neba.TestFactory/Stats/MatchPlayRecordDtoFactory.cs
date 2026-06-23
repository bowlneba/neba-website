using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Stats;

public static class MatchPlayRecordDtoFactory
{
    public const int ValidWins = 4;
    public const int ValidLosses = 2;
    public const decimal ValidWinPercentage = 66.67m;
    public const int ValidFinals = 3;
    public const decimal ValidMatchPlayAverage = 200.00m;
    public const decimal ValidWinnings = 2500m;

    public static MatchPlayRecordDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        int? wins = null,
        int? losses = null,
        decimal? winPercentage = null,
        int? finals = null,
        decimal? matchPlayAverage = null,
        decimal? winnings = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create(),
            Wins = wins ?? ValidWins,
            Losses = losses ?? ValidLosses,
            WinPercentage = winPercentage ?? ValidWinPercentage,
            Finals = finals ?? ValidFinals,
            MatchPlayAverage = matchPlayAverage ?? ValidMatchPlayAverage,
            Winnings = winnings ?? ValidWinnings
        };

    internal static IReadOnlyCollection<MatchPlayRecordDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);
        return [.. Enumerable.Range(0, count).Select(_ => new MatchPlayRecordDto
        {
            BowlerId = new BowlerId(Ulid.BogusString(faker)),
            BowlerName = bowlerNamePool.GetNext(),
            Wins = faker.Random.Int(0, 10),
            Losses = faker.Random.Int(0, 10),
            WinPercentage = faker.Random.Decimal(0, 100),
            Finals = faker.Random.Int(1, 15),
            MatchPlayAverage = faker.Random.Decimal(150, 230),
            Winnings = faker.Random.Decimal(0, 5000)
        })];
    }

    public static IReadOnlyCollection<MatchPlayRecordDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}