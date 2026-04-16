using Bogus;

using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Bowlers;

namespace Neba.TestFactory.Stats;

public static class SeasonStatsSummaryDtoFactory
{
    public const int ValidTotalEntries = 120;
    public const int ValidTotalPrizeMoney = 12500;
    public const int ValidHighGame = 279;
    public const int ValidHighBlock = 1250;
    public const decimal ValidHighAverage = 225.35m;
    public const decimal ValidHighestMatchPlayWinPercentage = 65.5m;
    public const int ValidMostFinals = 8;
    public const string ValidBowlerName = "Test Bowler";

    public static SeasonStatsSummaryDto Create(
        int? totalEntries = null,
        int? totalPrizeMoney = null,
        int? highGame = null,
        IReadOnlyDictionary<BowlerId, string>? highGameBowlers = null,
        int? highBlock = null,
        IReadOnlyDictionary<BowlerId, string>? highBlockBowlers = null,
        decimal? highAverage = null,
        IReadOnlyDictionary<BowlerId, string>? highAverageBowlers = null,
        decimal? highestMatchPlayWinPercentage = null,
        IReadOnlyDictionary<BowlerId, string>? highestMatchPlayWinPercentageBowlers = null,
        int? mostFinals = null,
        IReadOnlyDictionary<BowlerId, string>? mostFinalsBowlers = null)
        => new()
        {
            TotalEntries = totalEntries ?? ValidTotalEntries,
            TotalPrizeMoney = totalPrizeMoney ?? ValidTotalPrizeMoney,
            HighGame = highGame ?? ValidHighGame,
            HighGameBowlers = highGameBowlers ?? new Dictionary<BowlerId, string> { { BowlerId.New(), ValidBowlerName } },
            HighBlock = highBlock ?? ValidHighBlock,
            HighBlockBowlers = highBlockBowlers ?? new Dictionary<BowlerId, string> { { BowlerId.New(), ValidBowlerName } },
            HighAverage = highAverage ?? ValidHighAverage,
            HighAverageBowlers = highAverageBowlers ?? new Dictionary<BowlerId, string> { { BowlerId.New(), ValidBowlerName } },
            HighestMatchPlayWinPercentage = highestMatchPlayWinPercentage ?? ValidHighestMatchPlayWinPercentage,
            HighestMatchPlayWinPercentageBowlers = highestMatchPlayWinPercentageBowlers ?? new Dictionary<BowlerId, string> { { BowlerId.New(), ValidBowlerName } },
            MostFinals = mostFinals ?? ValidMostFinals,
            MostFinalsBowlers = mostFinalsBowlers ?? new Dictionary<BowlerId, string> { { BowlerId.New(), ValidBowlerName } },
        };

    public static IReadOnlyCollection<SeasonStatsSummaryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SeasonStatsSummaryDto>()
            .CustomInstantiator(f => new SeasonStatsSummaryDto
            {
                TotalEntries = f.Random.Int(50, 500),
                TotalPrizeMoney = f.Random.Int(1000, 50000),
                HighGame = f.Random.Int(270, 300),
                HighGameBowlers = new Dictionary<BowlerId, string>
                {
                    { new BowlerId(Ulid.BogusString(f)), f.Name.FullName() },
                },
                HighBlock = f.Random.Int(1200, 1400),
                HighBlockBowlers = new Dictionary<BowlerId, string>
                {
                    { new BowlerId(Ulid.BogusString(f)), f.Name.FullName() },
                },
                HighAverage = f.Random.Decimal(210, 240),
                HighAverageBowlers = new Dictionary<BowlerId, string>
                {
                    { new BowlerId(Ulid.BogusString(f)), f.Name.FullName() },
                },
                HighestMatchPlayWinPercentage = f.Random.Decimal(50, 100),
                HighestMatchPlayWinPercentageBowlers = new Dictionary<BowlerId, string>
                {
                    { new BowlerId(Ulid.BogusString(f)), f.Name.FullName() },
                },
                MostFinals = f.Random.Int(1, 15),
                MostFinalsBowlers = new Dictionary<BowlerId, string>
                {
                    { new BowlerId(Ulid.BogusString(f)), f.Name.FullName() },
                },
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}