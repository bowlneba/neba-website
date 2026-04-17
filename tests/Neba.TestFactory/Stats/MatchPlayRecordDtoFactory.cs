using Bogus;

using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Bowlers;
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

    public static IReadOnlyCollection<MatchPlayRecordDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<MatchPlayRecordDto>()
            .CustomInstantiator(f => new MatchPlayRecordDto
            {
                BowlerId = new BowlerId(Ulid.BogusString(f)),
                BowlerName = NameFactory.Bogus(1, seed).Single(),
                Wins = f.Random.Int(0, 10),
                Losses = f.Random.Int(0, 10),
                WinPercentage = f.Random.Decimal(0, 100),
                Finals = f.Random.Int(1, 15),
                MatchPlayAverage = f.Random.Decimal(150, 230),
                Winnings = f.Random.Decimal(0, 5000)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
