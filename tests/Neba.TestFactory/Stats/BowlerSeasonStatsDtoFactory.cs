using System.Globalization;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Stats;

public static class BowlerSeasonStatsDtoFactory
{
    public const bool ValidIsMember = true;
    public const bool ValidIsRookie = false;
    public const bool ValidIsSenior = false;
    public const bool ValidIsSuperSenior = false;
    public const bool ValidIsWoman = false;
    public const bool ValidIsYouth = false;
    public const int ValidEligibleTournaments = 10;
    public const int ValidTotalTournaments = 12;
    public const int ValidEligibleEntries = 15;
    public const int ValidTotalEntries = 18;
    public const int ValidCashes = 8;
    public const int ValidFinals = 3;
    public const int ValidQualifyingHighGame = 289;
    public const int ValidHighBlock = 1150;
    public const int ValidMatchPlayWins = 4;
    public const int ValidMatchPlayLosses = 2;
    public const int ValidMatchPlayGames = 6;
    public const int ValidMatchPlayPinfall = 1200;
    public const int ValidMatchPlayHighGame = 255;
    public const int ValidTotalGames = 120;
    public const int ValidTotalPinfall = 22800;
    public const decimal ValidFieldAverage = 5.50m;
    public const int ValidBowlerOfTheYearPoints = 500;
    public const int ValidSeniorOfTheYearPoints = 0;
    public const int ValidSuperSeniorOfTheYearPoints = 0;
    public const int ValidWomanOfTheYearPoints = 0;
    public const int ValidYouthOfTheYearPoints = 0;
    public const decimal ValidTournamentWinnings = 2500m;
    public const decimal ValidCupEarnings = 500m;
    public const decimal ValidCredits = 100m;
    public static readonly DateTimeOffset ValidLastUpdatedUtc = new(2025, 9, 1, 0, 0, 0, TimeSpan.Zero);

    public static BowlerSeasonStatsDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        bool? isMember = null,
        bool? isRookie = null,
        bool? isSenior = null,
        bool? isSuperSenior = null,
        bool? isWoman = null,
        bool? isYouth = null,
        int? eligibleTournaments = null,
        int? totalTournaments = null,
        int? eligibleEntries = null,
        int? totalEntries = null,
        int? cashes = null,
        int? finals = null,
        int? qualifyingHighGame = null,
        int? highBlock = null,
        int? matchPlayWins = null,
        int? matchPlayLosses = null,
        int? matchPlayGames = null,
        int? matchPlayPinfall = null,
        int? matchPlayHighGame = null,
        int? totalGames = null,
        int? totalPinfall = null,
        decimal? fieldAverage = null,
        int? highFinish = null,
        decimal? averageFinish = null,
        int? bowlerOfTheYearPoints = null,
        int? seniorOfTheYearPoints = null,
        int? superSeniorOfTheYearPoints = null,
        int? womanOfTheYearPoints = null,
        int? youthOfTheYearPoints = null,
        decimal? tournamentWinnings = null,
        decimal? cupEarnings = null,
        decimal? credits = null,
        DateTimeOffset? lastUpdatedUtc = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create(),
            IsMember = isMember ?? ValidIsMember,
            IsRookie = isRookie ?? ValidIsRookie,
            IsSenior = isSenior ?? ValidIsSenior,
            IsSuperSenior = isSuperSenior ?? ValidIsSuperSenior,
            IsWoman = isWoman ?? ValidIsWoman,
            IsYouth = isYouth ?? ValidIsYouth,
            EligibleTournaments = eligibleTournaments ?? ValidEligibleTournaments,
            TotalTournaments = totalTournaments ?? ValidTotalTournaments,
            EligibleEntries = eligibleEntries ?? ValidEligibleEntries,
            TotalEntries = totalEntries ?? ValidTotalEntries,
            Cashes = cashes ?? ValidCashes,
            Finals = finals ?? ValidFinals,
            QualifyingHighGame = qualifyingHighGame ?? ValidQualifyingHighGame,
            HighBlock = highBlock ?? ValidHighBlock,
            MatchPlayWins = matchPlayWins ?? ValidMatchPlayWins,
            MatchPlayLosses = matchPlayLosses ?? ValidMatchPlayLosses,
            MatchPlayGames = matchPlayGames ?? ValidMatchPlayGames,
            MatchPlayPinfall = matchPlayPinfall ?? ValidMatchPlayPinfall,
            MatchPlayHighGame = matchPlayHighGame ?? ValidMatchPlayHighGame,
            TotalGames = totalGames ?? ValidTotalGames,
            TotalPinfall = totalPinfall ?? ValidTotalPinfall,
            FieldAverage = fieldAverage ?? ValidFieldAverage,
            HighFinish = highFinish,
            AverageFinish = averageFinish,
            BowlerOfTheYearPoints = bowlerOfTheYearPoints ?? ValidBowlerOfTheYearPoints,
            SeniorOfTheYearPoints = seniorOfTheYearPoints ?? ValidSeniorOfTheYearPoints,
            SuperSeniorOfTheYearPoints = superSeniorOfTheYearPoints ?? ValidSuperSeniorOfTheYearPoints,
            WomanOfTheYearPoints = womanOfTheYearPoints ?? ValidWomanOfTheYearPoints,
            YouthOfTheYearPoints = youthOfTheYearPoints ?? ValidYouthOfTheYearPoints,
            TournamentWinnings = tournamentWinnings ?? ValidTournamentWinnings,
            CupEarnings = cupEarnings ?? ValidCupEarnings,
            Credits = credits ?? ValidCredits,
            LastUpdatedUtc = lastUpdatedUtc ?? ValidLastUpdatedUtc,
        };

    public static IReadOnlyCollection<BowlerSeasonStatsDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var mpWins = faker.Random.Int(0, 15);
            var mpLosses = faker.Random.Int(0, 10);
            var mpGames = mpWins + mpLosses;
            var mpPinfall = mpGames * faker.Random.Int(150, 280);

            var tournaments = faker.Random.Int(1, 20);
            var totalTournaments = tournaments + faker.Random.Int(0, 5);
            var entries = faker.Random.Int(tournaments, tournaments * 2);
            var totalEntries = entries + faker.Random.Int(0, 5);
            var finals = faker.Random.Int(0, entries);
            var cashes = faker.Random.Int(finals, entries);

            var totalGames = faker.Random.Int(60, 300);
            var totalPinfall = totalGames * faker.Random.Int(150, 280);

            return new BowlerSeasonStatsDto
            {
                BowlerId = BowlerId.Parse(Ulid.BogusString(faker, faker.Date.Past()), CultureInfo.InvariantCulture),
                BowlerName = bowlerNamePool.GetNext(),
                IsMember = faker.Random.Bool(),
                IsRookie = faker.Random.Bool(),
                IsSenior = faker.Random.Bool(),
                IsSuperSenior = faker.Random.Bool(),
                IsWoman = faker.Random.Bool(),
                IsYouth = faker.Random.Bool(),
                EligibleTournaments = tournaments,
                TotalTournaments = totalTournaments,
                EligibleEntries = entries,
                TotalEntries = totalEntries,
                Cashes = cashes,
                Finals = finals,
                QualifyingHighGame = faker.Random.Int(200, 300),
                HighBlock = faker.Random.Int(800, 1400),
                MatchPlayWins = mpWins,
                MatchPlayLosses = mpLosses,
                MatchPlayGames = mpGames,
                MatchPlayPinfall = mpPinfall,
                MatchPlayHighGame = faker.Random.Int(180, 300),
                TotalGames = totalGames,
                TotalPinfall = totalPinfall,
                FieldAverage = faker.Random.Decimal(-20, 30),
                HighFinish = faker.Random.Bool() ? faker.Random.Int(1, 10) : null,
                AverageFinish = faker.Random.Bool() ? faker.Random.Decimal(1, 50) : null,
                BowlerOfTheYearPoints = faker.Random.Int(0, 2000),
                SeniorOfTheYearPoints = faker.Random.Int(0, 2000),
                SuperSeniorOfTheYearPoints = faker.Random.Int(0, 2000),
                WomanOfTheYearPoints = faker.Random.Int(0, 2000),
                YouthOfTheYearPoints = faker.Random.Int(0, 2000),
                TournamentWinnings = faker.Random.Decimal(0, 10000),
                CupEarnings = faker.Random.Decimal(0, 5000),
                Credits = faker.Random.Decimal(0, 1000),
                LastUpdatedUtc = faker.Date.PastOffset(2),
            };
        })];
    }

    public static IReadOnlyCollection<BowlerSeasonStatsDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}