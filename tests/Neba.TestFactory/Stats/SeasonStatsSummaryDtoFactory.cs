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

    internal static IReadOnlyCollection<SeasonStatsSummaryDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new SeasonStatsSummaryDto
        {
            TotalEntries = faker.Random.Int(50, 500),
            TotalPrizeMoney = faker.Random.Decimal(1000, 50000),
            HighGame = faker.Random.Int(270, 300),
            HighGameBowlers = new Dictionary<BowlerId, Name>
            {
                { new BowlerId(Ulid.BogusString(faker)), NameFactory.Bogus(1, faker).Single() },
            },
            HighBlock = faker.Random.Int(1200, 1400),
            HighBlockBowlers = new Dictionary<BowlerId, Name>
            {
                { new BowlerId(Ulid.BogusString(faker)), NameFactory.Bogus(1, faker).Single() },
            },
            HighAverage = faker.Random.Decimal(210, 240),
            HighAverageBowlers = new Dictionary<BowlerId, Name>
            {
                { new BowlerId(Ulid.BogusString(faker)), NameFactory.Bogus(1, faker).Single() },
            },
            HighestMatchPlayWinPercentage = faker.Random.Decimal(50, 100),
            HighestMatchPlayWinPercentageBowlers = new Dictionary<BowlerId, Name>
            {
                { new BowlerId(Ulid.BogusString(faker)), NameFactory.Bogus(1, faker).Single() },
            },
            MostFinals = faker.Random.Int(1, 15),
            MostFinalsBowlers = new Dictionary<BowlerId, Name>
            {
                { new BowlerId(Ulid.BogusString(faker)), NameFactory.Bogus(1, faker).Single() },
            },
            BowlerOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(faker.Random.Int(5, 20), faker),
            SeniorOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(faker.Random.Int(1, 5), faker),
            SuperSeniorOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(faker.Random.Int(0, 3), faker),
            WomanOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(faker.Random.Int(1, 5), faker),
            RookieOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(faker.Random.Int(0, 5), faker),
            YouthOfTheYear = BowlerOfTheYearStandingDtoFactory.Bogus(faker.Random.Int(0, 3), faker),
            BowlerSearchList = BowlerSearchEntryDtoFactory.Bogus(faker.Random.Int(5, 20), faker),
            HighAverageLeaderboard = HighAverageDtoFactory.Bogus(faker.Random.Int(5, 20), faker),
            HighBlockLeaderboard = HighBlockDtoFactory.Bogus(faker.Random.Int(5, 20), faker),
            MatchPlayAverageLeaderboard = MatchPlayAverageDtoFactory.Bogus(faker.Random.Int(5, 20), faker),
            MatchPlayRecordLeaderboard = MatchPlayRecordDtoFactory.Bogus(faker.Random.Int(5, 20), faker),
            MatchPlayAppearancesLeaderboard = MatchPlayAppearancesDtoFactory.Bogus(faker.Random.Int(5, 20), faker),
            PointsPerEntryLeaderboard = PointsPerEntryDtoFactory.Bogus(faker.Random.Int(5, 20), faker),
            PointsPerTournamentLeaderboard = PointsPerTournamentDtoFactory.Bogus(faker.Random.Int(5, 20), faker),
            FinalsPerEntryLeaderboard = FinalsPerEntryDtoFactory.Bogus(faker.Random.Int(5, 20), faker),
            AverageFinishesLeaderboard = AverageFinishDtoFactory.Bogus(faker.Random.Int(5, 20), faker),
            AllBowlers = FullStatModalRowDtoFactory.Bogus(faker.Random.Int(10, 30), faker)
        })];
    }

    public static IReadOnlyCollection<SeasonStatsSummaryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}