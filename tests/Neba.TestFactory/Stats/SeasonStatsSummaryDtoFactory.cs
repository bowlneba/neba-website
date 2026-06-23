using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;
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
            .CustomInstantiator(f => new SeasonStatsSummaryDto
            {
                TotalEntries = f.Random.Int(50, 500),
                TotalPrizeMoney = f.Random.Decimal(1000, 50000),
                HighGame = f.Random.Int(270, 300),
                HighGameBowlers = new Dictionary<BowlerId, Name>
                {
                    { new BowlerId(Ulid.BogusString(f)), NameFactory.Bogus(1, f).Single() },
                },
                HighBlock = f.Random.Int(1200, 1400),
                HighBlockBowlers = new Dictionary<BowlerId, Name>
                {
                    { new BowlerId(Ulid.BogusString(f)), NameFactory.Bogus(1, f).Single() },
                },
                HighAverage = f.Random.Decimal(210, 240),
                HighAverageBowlers = new Dictionary<BowlerId, Name>
                {
                    { new BowlerId(Ulid.BogusString(f)), NameFactory.Bogus(1, f).Single() },
                },
                HighestMatchPlayWinPercentage = f.Random.Decimal(50, 100),
                HighestMatchPlayWinPercentageBowlers = new Dictionary<BowlerId, Name>
                {
                    { new BowlerId(Ulid.BogusString(f)), NameFactory.Bogus(1, f).Single() },
                },
                MostFinals = f.Random.Int(1, 15),
                MostFinalsBowlers = new Dictionary<BowlerId, Name>
                {
                    { new BowlerId(Ulid.BogusString(f)), NameFactory.Bogus(1, f).Single() },
                },
                BowlerOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(f.Random.Int(5, 20), f),
                SeniorOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(f.Random.Int(1, 5), f),
                SuperSeniorOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(f.Random.Int(0, 3), f),
                WomanOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(f.Random.Int(1, 5), f),
                RookieOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(f.Random.Int(0, 5), f),
                YouthOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(f.Random.Int(0, 3), f),
                BowlerSearchList = BowlerSearchEntryDtoFactory.Bogus(f.Random.Int(5, 20), f),
                HighAverageLeaderboard = HighAverageDtoFactory.Bogus(f.Random.Int(5, 20), f),
                HighBlockLeaderboard = HighBlockDtoFactory.Bogus(f.Random.Int(5, 20), f),
                MatchPlayAverageLeaderboard = MatchPlayAverageDtoFactory.Bogus(f.Random.Int(5, 20), f),
                MatchPlayRecordLeaderboard = MatchPlayRecordDtoFactory.Bogus(f.Random.Int(5, 20), f),
                MatchPlayAppearancesLeaderboard = MatchPlayAppearancesDtoFactory.Bogus(f.Random.Int(5, 20), f),
                PointsPerEntryLeaderboard = PointsPerEntryDtoFactory.Bogus(f.Random.Int(5, 20), f),
                PointsPerTournamentLeaderboard = PointsPerTournamentDtoFactory.Bogus(f.Random.Int(5, 20), f),
                FinalsPerEntryLeaderboard = FinalsPerEntryDtoFactory.Bogus(f.Random.Int(5, 20), f),
                AverageFinishesLeaderboard = AverageFinishDtoFactory.Bogus(f.Random.Int(5, 20), f),
                AllBowlers = FullStatModalRowDtoFactory.Bogus(f.Random.Int(10, 30), f)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<SeasonStatsSummaryDto> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}
