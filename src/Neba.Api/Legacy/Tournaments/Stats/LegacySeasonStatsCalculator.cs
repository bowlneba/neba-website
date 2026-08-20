namespace Neba.Api.Legacy.Tournaments.Stats;

// Pure mapping logic - no I/O - so it's unit-testable in isolation from GenerateSeasonStatsJob's
// Dapper/EF plumbing, mirroring TournamentPlaceCalculator's split from SyncTournamentResultsJob.
//
// Verified against nebamgmt-v3's actual, live report source - Reporting/NEBA.Reporting.Data/
// Tournaments/Stats/TournamentStatsRepository.cs's Dump(DateTime asOfDate) method (not just the
// Docs/GetBowlerSeasonStats.cs draft, which is close but omits the SeniorWithWomen contribution
// below) - field by field, cross-checked against Docs/tournament-stats.md's written spec. Adjusted
// for the Place/PrizeMoney/Points-from-TournamentResult decision (see the plan's Decision Recap):
// award-points formulas below operate on a single BowlerId+TournamentId result row (TournamentResult
// only ever holds one), same shape live Dump's own "one ResultsStats row per bowler per tournament"
// convention assumes - a SideCut value is an attribute of that one row (which cut got the bowler
// into finals), not a signal that multiple rows exist for the same tournament.
//
// Several fields below are intentionally NOT scoped to eligible tournaments even though the live
// Dump scopes them (Cashes, Finals, QualifyingHighGame, HighBlock, the MatchPlay* fields, TotalGames,
// TotalPinfall, HighFinish, AverageFinish) - BowlerSeasonStats's own XML docs describe these as
// spanning the whole season, and (unlike Tournaments/Entries) there's no separate Eligible/Total pair
// for any of them - the website's own aggregate deliberately simplifies away this legacy nuance.
internal static class LegacySeasonStatsCalculator
{
    private const int SeniorAge = 50;
    private const int SuperSeniorAge = 60;
    private const int YouthAge = 18;

    // SideCutPointsValue int values (BOM/NEBA.BOM/Tournaments/Stats/ResultsStats.cs). Senior (1) has
    // no dedicated constant - it needs no explicit check, since Senior points include every side cut
    // except SuperSenior/Woman (checked below), and Senior itself is never excluded from its own award.
    private const int SuperSeniorSideCut = 2;
    private const int WomanSideCut = 3;
    private const int CombinedSideCut = 999;

    // SinglesTournamentTypes int values (BOM/NEBA.BOM/Tournaments/Singles/SinglesTournamentTypes.cs).
    private const int NonChampionsType = 1;
    private const int SeniorType = 2;
    private const int WomenType = 3;
    private const int ChampionsType = 4;
    private const int YouthType = 7;
    private const int SeniorWithWomenType = 8;

    // Historical rule: a bowler who advances to finals via any side cut earns a flat bonus (instead
    // of placement points) toward Bowler of the Year, on top of full placement points toward their
    // own divisional award. No longer produces nonzero output for current seasons (SideCut is only
    // ever populated for old data), but the live Dump still computes it this way - preserved as-is.
    private const int SideCutFinalsBonusPoints = 5;

    public static IReadOnlyCollection<LegacyBowlerSeasonStatsResult> Compute(
        DateOnly seasonEndDate,
        int newMembershipTypeId,
        IReadOnlyCollection<LegacySeasonTournamentRow> seasonTournaments,
        IReadOnlyCollection<LegacyQualifyingStatsRow> qualifyingStats,
        IReadOnlyCollection<LegacyMatchPlayStatsRow> matchPlayStats,
        IReadOnlyCollection<LegacyBowlerResultRow> results,
        IReadOnlyCollection<LegacyBowlerRow> bowlers,
        IReadOnlyCollection<LegacyMembershipRow> memberships,
        IReadOnlyCollection<LegacyCreditRow> credits,
        IReadOnlyCollection<LegacyCupResultRow> cupResults)
    {
        var eligibleSeasonTournaments = seasonTournaments.Where(t => t.YearlyStatEligible).ToList();
        var eligibleTournamentIds = eligibleSeasonTournaments.Select(t => t.TournamentId).ToHashSet();

        var seniorTournaments = seasonTournaments.Where(t => t.SinglesTournamentType == SeniorType).ToList();
        var youthTournaments = seasonTournaments.Where(t => t.SinglesTournamentType == YouthType).ToList();
        var womenTournamentIds = seasonTournaments
            .Where(t => t.SinglesTournamentType == WomenType)
            .Select(t => t.TournamentId).ToHashSet();
        var seniorWithWomenTournaments = seasonTournaments.Where(t => t.SinglesTournamentType == SeniorWithWomenType).ToList();

        var excludedTournamentIdByBowlerId = ComputeTournamentOfChampionsDoubleDipExclusions(seasonTournaments, results);

        var bowlerIds = qualifyingStats.Select(q => q.BowlerId)
            .Concat(matchPlayStats.Select(m => m.BowlerId))
            .Concat(results.Select(r => r.BowlerId))
            .Distinct()
            .ToList();

        var qualifyingByBowlerId = qualifyingStats.GroupBy(q => q.BowlerId).ToDictionary(g => g.Key, g => g.ToList());
        var matchPlayByBowlerId = matchPlayStats.GroupBy(m => m.BowlerId).ToDictionary(g => g.Key, g => g.ToList());
        var resultsByBowlerId = results.GroupBy(r => r.BowlerId).ToDictionary(g => g.Key, g => g.ToList());
        var bowlerById = bowlers.ToDictionary(b => b.BowlerId);
        var membershipsByBowlerId = memberships.GroupBy(m => m.BowlerId).ToDictionary(g => g.Key, g => g.ToList());
        var creditsByBowlerId = credits.GroupBy(c => c.BowlerId).ToDictionary(g => g.Key, g => g.ToList());
        var cupResultsByBowlerId = cupResults.GroupBy(c => c.BowlerId).ToDictionary(g => g.Key, g => g.ToList());

        var computedResults = new List<LegacyBowlerSeasonStatsResult>();

        foreach (var bowlerId in bowlerIds)
        {
            var qualifying = qualifyingByBowlerId.GetValueOrDefault(bowlerId, []);
            var matchPlay = matchPlayByBowlerId.GetValueOrDefault(bowlerId, []);
            var bowlerResults = resultsByBowlerId.GetValueOrDefault(bowlerId, []);
            var excludedTournamentId = excludedTournamentIdByBowlerId.GetValueOrDefault(bowlerId);

            var bowler = bowlerById.GetValueOrDefault(bowlerId);
            var dateOfBirth = bowler?.DateOfBirth;

            // Participation ("entered a tournament") is any of the three stat types, matching the
            // live Dump's bowler.Stats.Select(TournamentId) (which spans all stat subtypes, not
            // just qualifying). The TOC double-dip exclusion only ever removes an id from the
            // *eligible* set below, never from these totals - matching the live source exactly.
            var participatedTournamentIds = qualifying.Select(q => q.TournamentId)
                .Concat(matchPlay.Select(m => m.TournamentId))
                .Concat(bowlerResults.Select(r => r.TournamentId))
                .Distinct()
                .ToList();

            var eligibleTournamentIdsForBowler = participatedTournamentIds
                .Where(id => id != excludedTournamentId && eligibleTournamentIds.Contains(id))
                .ToList();

            var totalEntries = qualifying.Count;
            var eligibleEntries = qualifying.Count(q => q.TournamentId != excludedTournamentId && eligibleTournamentIds.Contains(q.TournamentId));

            var age = AgeOnDate(dateOfBirth, seasonEndDate);
            var isSenior = age >= SeniorAge;
            var isSuperSenior = age >= SuperSeniorAge;
            var isWoman = bowler?.Gender == 1;

            var bowlerMemberships = membershipsByBowlerId.GetValueOrDefault(bowlerId, []);
            var isMember = bowlerMemberships.Any(m => m.EndDate == seasonEndDate);
            var isRookie = isMember
                && bowlerMemberships
                    .OrderByDescending(m => m.EndDate)
                    .First().MembershipId == newMembershipTypeId;

            var cashes = bowlerResults.Count(r => r.PrizeMoney > 0);
            var finals = matchPlay.Select(m => m.TournamentId).Distinct().Count();

            var totalGames = qualifying.Sum(q => q.Games) + matchPlay.Sum(m => m.Games);
            var totalPinfall = qualifying.Sum(q => q.Score) + matchPlay.Sum(m => m.Score);

            var fieldAverage = ComputeFieldAverage(eligibleTournamentIdsForBowler, qualifying, qualifyingStats);

            var qualifyingHighGame = qualifying.Count > 0 ? qualifying.Max(q => q.HighGame) : 0;
            var highBlock = qualifying.Where(q => q.Games == 5).Select(q => (int?)q.Score).Max() ?? 0;

            var matchPlayWins = matchPlay.Count(m => m.Winner);
            var matchPlayLosses = matchPlay.Count(m => !m.Winner);
            var matchPlayGames = matchPlay.Sum(m => m.Games);
            var matchPlayPinfall = matchPlay.Sum(m => m.Score);
            var matchPlayHighGame = matchPlay.Count > 0 ? matchPlay.Max(m => m.HighGame) : 0;

            int? highFinish = bowlerResults.Count > 0 ? bowlerResults.Min(r => r.Place) : null;
            decimal? averageFinish = bowlerResults.Count > 0 ? (decimal)bowlerResults.Average(r => r.Place) : null;

            // Bowler of the Year: placement points from the main cut only (SideCut is null), plus
            // a flat bonus per finals appearance reached via any side cut.
            var eligibleResults = bowlerResults
                .Where(r => r.TournamentId != excludedTournamentId && eligibleTournamentIds.Contains(r.TournamentId))
                .ToList();
            var rawBowlerOfTheYearPoints = eligibleResults.Where(r => r.SideCut is null).Sum(r => r.Points);
            var finalsViaSideCut = eligibleResults.Count(r => r.SideCut is not null);
            var bowlerOfTheYearPoints = rawBowlerOfTheYearPoints + finalsViaSideCut * SideCutFinalsBonusPoints;

            // Senior/Super Senior of the Year: eligibility is per-tournament, not per-season - a
            // bowler who turns 50/60 partway through the season only qualifies for tournaments
            // whose end date falls on or after that birthday. The eligible set is the union of
            // ordinary eligible tournaments and Senior-type tournaments (open to 50+ regardless of
            // YearlyStatEligible), each filtered by the bowler's age as of that tournament's end.
            var seniorEligibleTournamentIds = ComputeAgeEligibleTournamentIds(eligibleSeasonTournaments, seniorTournaments, dateOfBirth, SeniorAge);
            var superSeniorEligibleTournamentIds = ComputeAgeEligibleTournamentIds(eligibleSeasonTournaments, seniorTournaments, dateOfBirth, SuperSeniorAge);
            var seniorWithWomenSeniorTournamentIds = ComputeAgeEligibleTournamentIds([], seniorWithWomenTournaments, dateOfBirth, SeniorAge);
            var seniorWithWomenSuperSeniorTournamentIds = ComputeAgeEligibleTournamentIds([], seniorWithWomenTournaments, dateOfBirth, SuperSeniorAge);

            var seniorResults = bowlerResults.Where(r => seniorEligibleTournamentIds.Contains(r.TournamentId)).ToList();
            var seniorFinalsViaSuperSeniorCut = seniorResults.Count(r => r.SideCut == SuperSeniorSideCut);
            var seniorWithWomenSeniorPoints = bowlerResults.Where(r => seniorWithWomenSeniorTournamentIds.Contains(r.TournamentId)).Sum(r => r.Points);
            var seniorOfTheYearPoints = seniorResults
                .Where(r => r.SideCut != SuperSeniorSideCut && r.SideCut != WomanSideCut)
                .Sum(r => r.Points)
                + seniorFinalsViaSuperSeniorCut * SideCutFinalsBonusPoints
                + seniorWithWomenSeniorPoints;

            var superSeniorResults = bowlerResults.Where(r => superSeniorEligibleTournamentIds.Contains(r.TournamentId)).ToList();
            var seniorWithWomenSuperSeniorPoints = bowlerResults.Where(r => seniorWithWomenSuperSeniorTournamentIds.Contains(r.TournamentId)).Sum(r => r.Points);
            var superSeniorOfTheYearPoints = superSeniorResults
                .Where(r => r.SideCut != WomanSideCut)
                .Sum(r => r.Points)
                + seniorWithWomenSuperSeniorPoints;

            // Woman of the Year: the bowler's own Bowler of the Year points (main-cut only), plus
            // points earned via a Woman or Combined side cut, plus points from women-specific
            // tournaments (any tournament of TournamentType Women, whether eligible or not).
            var womanSideCutPoints = eligibleResults.Where(r => r.SideCut is WomanSideCut or CombinedSideCut).Sum(r => r.Points);
            var womanTournamentPoints = bowlerResults.Where(r => womenTournamentIds.Contains(r.TournamentId)).Sum(r => r.Points);
            var womanOfTheYearPoints = isWoman ? rawBowlerOfTheYearPoints + womanSideCutPoints + womanTournamentPoints : 0;

            // Youth of the Year: same per-tournament age-eligibility shape as Senior/Super Senior,
            // but under 18 rather than at-or-over a floor. IsYouth is derived from whether the
            // bowler actually earned any youth points, not from raw age - a bowler under 18 who
            // never entered a youth-eligible tournament (or scored zero) is not flagged Youth.
            var youthEligibleTournamentIds = ComputeAgeEligibleTournamentIds(eligibleSeasonTournaments, youthTournaments, dateOfBirth, maximumAgeExclusive: YouthAge);
            var youthOfTheYearPoints = bowlerResults.Where(r => youthEligibleTournamentIds.Contains(r.TournamentId)).Sum(r => r.Points);
            var isYouth = youthOfTheYearPoints > 0;

            var tournamentWinnings = bowlerResults.Sum(r => r.PrizeMoney);
            var cupEarnings = cupResultsByBowlerId.GetValueOrDefault(bowlerId, [])
                .Where(c => c.CupEnd.Year == seasonEndDate.Year)
                .Sum(c => c.Payout);
            var bowlerCredits = creditsByBowlerId.GetValueOrDefault(bowlerId, []).Sum(c => c.Amount);

            computedResults.Add(new LegacyBowlerSeasonStatsResult(
                BowlerId: bowlerId,
                IsMember: isMember,
                IsRookie: isRookie,
                IsSenior: isSenior,
                IsSuperSenior: isSuperSenior,
                IsWoman: isWoman,
                IsYouth: isYouth,
                EligibleTournaments: eligibleTournamentIdsForBowler.Count,
                TotalTournaments: participatedTournamentIds.Count,
                EligibleEntries: eligibleEntries,
                TotalEntries: totalEntries,
                Cashes: cashes,
                Finals: finals,
                TotalGames: totalGames,
                TotalPinfall: totalPinfall,
                FieldAverage: fieldAverage,
                QualifyingHighGame: qualifyingHighGame,
                HighBlock: highBlock,
                HighFinish: highFinish,
                AverageFinish: averageFinish,
                MatchPlayWins: matchPlayWins,
                MatchPlayLosses: matchPlayLosses,
                MatchPlayGames: matchPlayGames,
                MatchPlayPinfall: matchPlayPinfall,
                MatchPlayHighGame: matchPlayHighGame,
                BowlerOfTheYearPoints: bowlerOfTheYearPoints,
                SeniorOfTheYearPoints: seniorOfTheYearPoints,
                SuperSeniorOfTheYearPoints: superSeniorOfTheYearPoints,
                WomanOfTheYearPoints: womanOfTheYearPoints,
                YouthOfTheYearPoints: youthOfTheYearPoints,
                TournamentWinnings: tournamentWinnings,
                CupEarnings: cupEarnings,
                Credits: bowlerCredits,
                LastUpdatedUtc: DateTimeOffset.UtcNow));
        }

        return computedResults;
    }

    // Union of the base eligible tournament set and a category-specific tournament set (Senior,
    // Youth, or SeniorWithWomen), filtered to the tournaments where the bowler's age as of that
    // specific tournament's end date satisfies the category's age bound. Pass minimumAgeInclusive
    // for Senior/Super Senior ("50+"/"60+"), or maximumAgeExclusive for Youth ("under 18").
    private static HashSet<int> ComputeAgeEligibleTournamentIds(
        IReadOnlyCollection<LegacySeasonTournamentRow> baseEligibleTournaments,
        IReadOnlyCollection<LegacySeasonTournamentRow> categoryTournaments,
        DateOnly? dateOfBirth,
        int? minimumAgeInclusive = null,
        int? maximumAgeExclusive = null)
        => baseEligibleTournaments
            .Concat(categoryTournaments)
            .DistinctBy(t => t.TournamentId)
            .Where(t => AgeOnDate(dateOfBirth, DateOnly.FromDateTime(t.End)) is { } age
                && (minimumAgeInclusive is not { } min || age >= min)
                && (maximumAgeExclusive is not { } max || age < max))
            .Select(t => t.TournamentId)
            .ToHashSet();

    private static decimal ComputeFieldAverage(
        List<int> eligibleTournamentIdsForBowler,
        IReadOnlyCollection<LegacyQualifyingStatsRow> bowlerQualifying,
        IReadOnlyCollection<LegacyQualifyingStatsRow> allQualifying)
    {
        if (eligibleTournamentIdsForBowler.Count == 0)
        {
            return 0m;
        }

        var bowlerRows = bowlerQualifying.Where(q => eligibleTournamentIdsForBowler.Contains(q.TournamentId)).ToList();
        var bowlerGames = bowlerRows.Sum(q => q.Games);
        if (bowlerGames == 0)
        {
            return 0m;
        }

        var bowlerAverage = (decimal)bowlerRows.Sum(q => q.Score) / bowlerGames;

        var fieldRows = allQualifying.Where(q => eligibleTournamentIdsForBowler.Contains(q.TournamentId)).ToList();
        var fieldGames = fieldRows.Sum(q => q.Games);
        var fieldAverage = fieldGames == 0 ? 0m : (decimal)fieldRows.Sum(q => q.Score) / fieldGames;

        return bowlerAverage - fieldAverage;
    }

    // The Non-Champions event's single-day winner earns a Tournament of Champions berth that
    // season - a forced entry that should not count as a second, ordinary tournament for that
    // bowler's *eligible* tournament/entry counts and eligible-results-based points (Bowler of the
    // Year). It does not affect Total tournament/entry counts, or any of the Senior/Super
    // Senior/Woman/Youth category formulas - matching the live Dump exactly.
    private static Dictionary<int, int> ComputeTournamentOfChampionsDoubleDipExclusions(
        IReadOnlyCollection<LegacySeasonTournamentRow> seasonTournaments,
        IReadOnlyCollection<LegacyBowlerResultRow> results)
    {
        var nonChampionsTournament = seasonTournaments
            .SingleOrDefault(t => t.SinglesTournamentType == NonChampionsType && t.Start.Date == t.End.Date);
        var tournamentOfChampions = seasonTournaments.SingleOrDefault(t => t.SinglesTournamentType == ChampionsType);

        if (nonChampionsTournament is null || tournamentOfChampions is null)
        {
            return [];
        }

        var winner = results
            .Where(r => r.TournamentId == nonChampionsTournament.TournamentId && r.Place == 1)
            .Select(r => (int?)r.BowlerId)
            .SingleOrDefault();

        return winner.HasValue
            ? new Dictionary<int, int> { [winner.Value] = tournamentOfChampions.TournamentId }
            : [];
    }

    // Ported from Data/NEBA.Data/EntityExtensionMethods.cs's AgeOnDate - returns null when the
    // bowler has no date of birth on file (matching the legacy null-DOB exclusion), rather than
    // treating a missing DOB as age zero.
    private static int? AgeOnDate(DateOnly? dateOfBirth, DateOnly asOf)
    {
        if (dateOfBirth is not { } dob)
        {
            return null;
        }

        var age = asOf.Year - dob.Year;
        if (dob > asOf.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
