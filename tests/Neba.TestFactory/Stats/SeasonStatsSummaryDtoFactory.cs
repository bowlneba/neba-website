using Bogus;

using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Bowlers;
using Neba.TestFactory.Bowlers;

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

    public static SeasonStatsSummaryDto Create(
        int? totalEntries = null,
        decimal? totalPrizeMoney = null,
        int? highGame = null,
        IReadOnlyDictionary<BowlerId, Name>? highGameBowlers = null,
        int? highBlock = null,
        IReadOnlyDictionary<BowlerId, Name>? highBlockBowlers = null,
        decimal? highAverage = null,
        IReadOnlyDictionary<BowlerId, Name>? highAverageBowlers = null,
        decimal? highestMatchPlayWinPercentage = null,
        IReadOnlyDictionary<BowlerId, Name>? highestMatchPlayWinPercentageBowlers = null,
        int? mostFinals = null,
        IReadOnlyDictionary<BowlerId, Name>? mostFinalsBowlers = null,
        IReadOnlyCollection<BowlerOfTheYearStandingDto>? bowlerOfTheYear = null,
        IReadOnlyCollection<BowlerOfTheYearStandingDto>? seniorOfTheYear = null,
        IReadOnlyCollection<BowlerOfTheYearStandingDto>? superSeniorOfTheYear = null,
        IReadOnlyCollection<BowlerOfTheYearStandingDto>? womanOfTheYear = null,
        IReadOnlyCollection<BowlerOfTheYearStandingDto>? rookieOfTheYear = null,
        IReadOnlyCollection<BowlerOfTheYearStandingDto>? youthOfTheYear = null,
        IReadOnlyCollection<BowlerSearchEntryDto>? bowlerSearchList = null,
        IReadOnlyCollection<HighAverageDto>? highAverageLeaderboard = null,
        IReadOnlyCollection<HighBlockDto>? highBlockLeaderboard = null,
        IReadOnlyCollection<MatchPlayAverageDto>? matchPlayAverageLeaderboard = null,
        IReadOnlyCollection<MatchPlayRecordDto>? matchPlayRecordLeaderboard = null,
        IReadOnlyCollection<MatchPlayAppearancesDto>? matchPlayAppearancesLeaderboard = null,
        IReadOnlyCollection<PointsPerEntryDto>? pointsPerEntryLeaderboard = null,
        IReadOnlyCollection<PointsPerTournamentDto>? pointsPerTournamentLeaderboard = null,
        IReadOnlyCollection<FinalsPerEntryDto>? finalsPerEntryLeaderboard = null,
        IReadOnlyCollection<AverageFinishDto>? averageFinishesLeaderboard = null,
        IReadOnlyCollection<FullStatModalRowDto>? allBowlers = null)
        => new()
        {
            TotalEntries = totalEntries ?? ValidTotalEntries,
            TotalPrizeMoney = totalPrizeMoney ?? ValidTotalPrizeMoney,
            HighGame = highGame ?? ValidHighGame,
            HighGameBowlers = highGameBowlers ?? new Dictionary<BowlerId, Name> { { BowlerId.New(), NameFactory.Create() } },
            HighBlock = highBlock ?? ValidHighBlock,
            HighBlockBowlers = highBlockBowlers ?? new Dictionary<BowlerId, Name> { { BowlerId.New(), NameFactory.Create() } },
            HighAverage = highAverage ?? ValidHighAverage,
            HighAverageBowlers = highAverageBowlers ?? new Dictionary<BowlerId, Name> { { BowlerId.New(), NameFactory.Create() } },
            HighestMatchPlayWinPercentage = highestMatchPlayWinPercentage ?? ValidHighestMatchPlayWinPercentage,
            HighestMatchPlayWinPercentageBowlers = highestMatchPlayWinPercentageBowlers ?? new Dictionary<BowlerId, Name> { { BowlerId.New(), NameFactory.Create() } },
            MostFinals = mostFinals ?? ValidMostFinals,
            MostFinalsBowlers = mostFinalsBowlers ?? new Dictionary<BowlerId, Name> { { BowlerId.New(), NameFactory.Create() } },
            BowlerOfTheYear = bowlerOfTheYear ?? [BowlerOfTheYearStandingDtoFactory.Create()],
            SeniorOfTheYear = seniorOfTheYear ?? [],
            SuperSeniorOfTheYear = superSeniorOfTheYear ?? [],
            WomanOfTheYear = womanOfTheYear ?? [],
            RookieOfTheYear = rookieOfTheYear ?? [],
            YouthOfTheYear = youthOfTheYear ?? [],
            BowlerSearchList = bowlerSearchList ?? [BowlerSearchEntryDtoFactory.Create()],
            HighAverageLeaderboard = highAverageLeaderboard ?? [HighAverageDtoFactory.Create()],
            HighBlockLeaderboard = highBlockLeaderboard ?? [HighBlockDtoFactory.Create()],
            MatchPlayAverageLeaderboard = matchPlayAverageLeaderboard ?? [MatchPlayAverageDtoFactory.Create()],
            MatchPlayRecordLeaderboard = matchPlayRecordLeaderboard ?? [MatchPlayRecordDtoFactory.Create()],
            MatchPlayAppearancesLeaderboard = matchPlayAppearancesLeaderboard ?? [MatchPlayAppearancesDtoFactory.Create()],
            PointsPerEntryLeaderboard = pointsPerEntryLeaderboard ?? [PointsPerEntryDtoFactory.Create()],
            PointsPerTournamentLeaderboard = pointsPerTournamentLeaderboard ?? [PointsPerTournamentDtoFactory.Create()],
            FinalsPerEntryLeaderboard = finalsPerEntryLeaderboard ?? [FinalsPerEntryDtoFactory.Create()],
            AverageFinishesLeaderboard = averageFinishesLeaderboard ?? [AverageFinishDtoFactory.Create()],
            AllBowlers = allBowlers ?? [FullStatModalRowDtoFactory.Create()]
        };

    public static IReadOnlyCollection<SeasonStatsSummaryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SeasonStatsSummaryDto>()
            .CustomInstantiator(f =>
            {
                var highGameBowlerName = NameFactory.Bogus(1, f.Random.Int(1, int.MaxValue)).Single();
                var highBlockBowlerName = NameFactory.Bogus(1, f.Random.Int(1, int.MaxValue)).Single();
                var highAverageBowlerName = NameFactory.Bogus(1, f.Random.Int(1, int.MaxValue)).Single();
                var highestMatchPlayWinPercentageBowlerName = NameFactory.Bogus(1, f.Random.Int(1, int.MaxValue)).Single();
                var mostFinalsBowlerName = NameFactory.Bogus(1, f.Random.Int(1, int.MaxValue)).Single();
                var bowlerOfTheYearSeed = f.Random.Int(1, int.MaxValue);
                var seniorOfTheYearSeed = f.Random.Int(1, int.MaxValue);
                var superSeniorOfTheYearSeed = f.Random.Int(1, int.MaxValue);
                var womanOfTheYearSeed = f.Random.Int(1, int.MaxValue);
                var rookieOfTheYearSeed = f.Random.Int(1, int.MaxValue);
                var youthOfTheYearSeed = f.Random.Int(1, int.MaxValue);
                var bowlerSearchListSeed = f.Random.Int(1, int.MaxValue);
                var highAverageLeaderboardSeed = f.Random.Int(1, int.MaxValue);
                var highBlockLeaderboardSeed = f.Random.Int(1, int.MaxValue);
                var matchPlayAverageLeaderboardSeed = f.Random.Int(1, int.MaxValue);
                var matchPlayRecordLeaderboardSeed = f.Random.Int(1, int.MaxValue);
                var matchPlayAppearancesLeaderboardSeed = f.Random.Int(1, int.MaxValue);
                var pointsPerEntryLeaderboardSeed = f.Random.Int(1, int.MaxValue);
                var pointsPerTournamentLeaderboardSeed = f.Random.Int(1, int.MaxValue);
                var finalsPerEntryLeaderboardSeed = f.Random.Int(1, int.MaxValue);
                var averageFinishesLeaderboardSeed = f.Random.Int(1, int.MaxValue);
                var allBowlersSeed = f.Random.Int(1, int.MaxValue);

                return new SeasonStatsSummaryDto
                {
                    TotalEntries = f.Random.Int(50, 500),
                    TotalPrizeMoney = f.Random.Decimal(1000, 50000),
                    HighGame = f.Random.Int(270, 300),
                    HighGameBowlers = new Dictionary<BowlerId, Name>
                    {
                        { new BowlerId(Ulid.BogusString(f)), highGameBowlerName },
                    },
                    HighBlock = f.Random.Int(1200, 1400),
                    HighBlockBowlers = new Dictionary<BowlerId, Name>
                    {
                        { new BowlerId(Ulid.BogusString(f)), highBlockBowlerName },
                    },
                    HighAverage = f.Random.Decimal(210, 240),
                    HighAverageBowlers = new Dictionary<BowlerId, Name>
                    {
                        { new BowlerId(Ulid.BogusString(f)), highAverageBowlerName },
                    },
                    HighestMatchPlayWinPercentage = f.Random.Decimal(50, 100),
                    HighestMatchPlayWinPercentageBowlers = new Dictionary<BowlerId, Name>
                    {
                        { new BowlerId(Ulid.BogusString(f)), highestMatchPlayWinPercentageBowlerName },
                    },
                    MostFinals = f.Random.Int(1, 15),
                    MostFinalsBowlers = new Dictionary<BowlerId, Name>
                    {
                        { new BowlerId(Ulid.BogusString(f)), mostFinalsBowlerName },
                    },
                    BowlerOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(f.Random.Int(5, 20), bowlerOfTheYearSeed),
                    SeniorOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(f.Random.Int(1, 5), seniorOfTheYearSeed),
                    SuperSeniorOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(f.Random.Int(0, 3), superSeniorOfTheYearSeed),
                    WomanOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(f.Random.Int(1, 5), womanOfTheYearSeed),
                    RookieOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(f.Random.Int(0, 5), rookieOfTheYearSeed),
                    YouthOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(f.Random.Int(0, 3), youthOfTheYearSeed),
                    BowlerSearchList = BowlerSearchEntryDtoFactory.Bogus(f.Random.Int(5, 20), bowlerSearchListSeed),
                    HighAverageLeaderboard = HighAverageDtoFactory.Bogus(f.Random.Int(5, 20), highAverageLeaderboardSeed),
                    HighBlockLeaderboard = HighBlockDtoFactory.Bogus(f.Random.Int(5, 20), highBlockLeaderboardSeed),
                    MatchPlayAverageLeaderboard = MatchPlayAverageDtoFactory.Bogus(f.Random.Int(5, 20), matchPlayAverageLeaderboardSeed),
                    MatchPlayRecordLeaderboard = MatchPlayRecordDtoFactory.Bogus(f.Random.Int(5, 20), matchPlayRecordLeaderboardSeed),
                    MatchPlayAppearancesLeaderboard = MatchPlayAppearancesDtoFactory.Bogus(f.Random.Int(5, 20), matchPlayAppearancesLeaderboardSeed),
                    PointsPerEntryLeaderboard = PointsPerEntryDtoFactory.Bogus(f.Random.Int(5, 20), pointsPerEntryLeaderboardSeed),
                    PointsPerTournamentLeaderboard = PointsPerTournamentDtoFactory.Bogus(f.Random.Int(5, 20), pointsPerTournamentLeaderboardSeed),
                    FinalsPerEntryLeaderboard = FinalsPerEntryDtoFactory.Bogus(f.Random.Int(5, 20), finalsPerEntryLeaderboardSeed),
                    AverageFinishesLeaderboard = AverageFinishDtoFactory.Bogus(f.Random.Int(5, 20), averageFinishesLeaderboardSeed),
                    AllBowlers = FullStatModalRowDtoFactory.Bogus(f.Random.Int(10, 30), allBowlersSeed)
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}