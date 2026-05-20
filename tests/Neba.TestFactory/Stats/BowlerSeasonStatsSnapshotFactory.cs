using Neba.Api.Features.Stats.Domain;

namespace Neba.TestFactory.Stats;

public static class BowlerSeasonStatsSnapshotFactory
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
    public const int ValidMatchPlayWins = 4;
    public const int ValidMatchPlayLosses = 2;
    public const int ValidMatchPlayGames = 6;
    public const int ValidMatchPlayPinfall = 1200;
    public const int ValidTotalGames = 120;
    public const int ValidTotalPinfall = 22800;
    public const int ValidBowlerOfTheYearPoints = 500;
    public const int ValidSeniorOfTheYearPoints = 0;
    public const int ValidSuperSeniorOfTheYearPoints = 0;
    public const int ValidWomanOfTheYearPoints = 0;
    public const int ValidYouthOfTheYearPoints = 0;
    public const decimal ValidTournamentWinnings = 2500m;
    public const decimal ValidCupEarnings = 500m;
    public const decimal ValidCredits = 100m;

    public static BowlerSeasonStatsSnapshot Create(
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
        decimal? credits = null)
        => new()
        {
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
            QualifyingHighGame = qualifyingHighGame,
            HighBlock = highBlock,
            MatchPlayWins = matchPlayWins ?? ValidMatchPlayWins,
            MatchPlayLosses = matchPlayLosses ?? ValidMatchPlayLosses,
            MatchPlayGames = matchPlayGames ?? ValidMatchPlayGames,
            MatchPlayPinfall = matchPlayPinfall ?? ValidMatchPlayPinfall,
            MatchPlayHighGame = matchPlayHighGame,
            TotalGames = totalGames ?? ValidTotalGames,
            TotalPinfall = totalPinfall ?? ValidTotalPinfall,
            FieldAverage = fieldAverage,
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
        };

    public static IReadOnlyCollection<BowlerSeasonStatsSnapshot> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BowlerSeasonStatsSnapshot>()
            .CustomInstantiator(f =>
            {
                var mpWins = f.Random.Int(0, 15);
                var mpLosses = f.Random.Int(0, 10);
                var mpGames = mpWins + mpLosses;
                var mpPinfall = mpGames * f.Random.Int(150, 280);

                var tournaments = f.Random.Int(1, 20);
                var totalTournaments = tournaments + f.Random.Int(0, 5);
                var entries = f.Random.Int(tournaments, tournaments * 2);
                var totalEntries = entries + f.Random.Int(0, 5);
                var finals = f.Random.Int(0, entries);
                var cashes = f.Random.Int(finals, entries);

                var totalGames = f.Random.Int(60, 300);
                var totalPinfall = totalGames * f.Random.Int(150, 280);

                return new BowlerSeasonStatsSnapshot
                {
                    IsMember = f.Random.Bool(),
                    IsRookie = f.Random.Bool(),
                    IsSenior = f.Random.Bool(),
                    IsSuperSenior = f.Random.Bool(),
                    IsWoman = f.Random.Bool(),
                    IsYouth = f.Random.Bool(),
                    EligibleTournaments = tournaments,
                    TotalTournaments = totalTournaments,
                    EligibleEntries = entries,
                    TotalEntries = totalEntries,
                    Cashes = cashes,
                    Finals = finals,
                    QualifyingHighGame = f.Random.Bool() ? f.Random.Int(200, 300) : null,
                    HighBlock = f.Random.Bool() ? f.Random.Int(800, 1400) : null,
                    MatchPlayWins = mpWins,
                    MatchPlayLosses = mpLosses,
                    MatchPlayGames = mpGames,
                    MatchPlayPinfall = mpPinfall,
                    MatchPlayHighGame = f.Random.Bool() ? f.Random.Int(180, 300) : null,
                    TotalGames = totalGames,
                    TotalPinfall = totalPinfall,
                    FieldAverage = f.Random.Bool() ? f.Random.Decimal(-20, 30) : null,
                    HighFinish = f.Random.Bool() ? f.Random.Int(1, 10) : null,
                    AverageFinish = f.Random.Bool() ? f.Random.Decimal(1, 50) : null,
                    BowlerOfTheYearPoints = f.Random.Int(0, 2000),
                    SeniorOfTheYearPoints = f.Random.Int(0, 2000),
                    SuperSeniorOfTheYearPoints = f.Random.Int(0, 2000),
                    WomanOfTheYearPoints = f.Random.Int(0, 2000),
                    YouthOfTheYearPoints = f.Random.Int(0, 2000),
                    TournamentWinnings = f.Random.Decimal(0, 10000),
                    CupEarnings = f.Random.Decimal(0, 5000),
                    Credits = f.Random.Decimal(0, 1000),
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}