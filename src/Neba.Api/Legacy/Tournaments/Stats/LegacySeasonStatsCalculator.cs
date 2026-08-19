namespace Neba.Api.Legacy.Tournaments.Stats;

// Pure mapping logic - no I/O - so it's unit-testable in isolation from GenerateSeasonStatsJob's
// Dapper/EF plumbing, mirroring TournamentPlaceCalculator's split from SyncTournamentResultsJob.
//
// This is a port of nebamgmt-v3's own season-stats draft (Docs/GetBowlerSeasonStats.cs /
// Docs/tournament-stats.md), reshaped from EF navigation properties to plain row collections and
// adjusted for the Place/PrizeMoney/Points-from-TournamentResult decision (see the plan's Decision
// Recap). That source file was not available from this working directory at implementation time,
// so the formulas below are a best-effort reconstruction from the plan's field-by-field spec and
// this project's own BowlerSeasonStats XML documentation - re-check them against the actual
// nebamgmt-v3 draft (or the live Software report) before treating award-points output as final.
internal static class LegacySeasonStatsCalculator
{
    private const int SeniorAge = 50;
    private const int SuperSeniorAge = 60;
    private const int YouthAge = 18;
    private const int SeniorSideCut = 1;
    private const int SuperSeniorSideCut = 2;
    private const int WomanSideCut = 3;

    // SinglesTournamentTypes int values, from the legacy schema reference (SinglesTournamentTypes.cs).
    private const int NonChampionsType = 1;
    private const int ChampionsType = 4;

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
        var eligibleTournamentIds = seasonTournaments
            .Where(t => t.YearlyStatEligible)
            .Select(t => t.TournamentId)
            .ToHashSet();

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

            var totalTournamentIds = qualifying.Select(q => q.TournamentId)
                .Where(id => id != excludedTournamentId)
                .Distinct()
                .ToList();
            var eligibleTournamentIdsForBowler = totalTournamentIds.Where(eligibleTournamentIds.Contains).ToList();

            var totalEntries = qualifying.Count(q => q.TournamentId != excludedTournamentId);
            var eligibleEntries = qualifying.Count(q => q.TournamentId != excludedTournamentId && eligibleTournamentIds.Contains(q.TournamentId));

            var bowler = bowlerById.GetValueOrDefault(bowlerId);
            var age = AgeOnDate(bowler?.DateOfBirth, seasonEndDate);
            var isSenior = age >= SeniorAge;
            var isSuperSenior = age >= SuperSeniorAge;
            var isYouth = age.HasValue && age.Value < YouthAge;
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

            var eligibleResults = bowlerResults.Where(r => eligibleTournamentIds.Contains(r.TournamentId)).ToList();

            var bowlerOfTheYearPoints = eligibleResults.Sum(r => r.Points);
            var seniorOfTheYearPoints = isSenior
                ? SumCategoryPoints(eligibleResults, SeniorSideCut)
                : 0;
            var superSeniorOfTheYearPoints = isSuperSenior
                ? SumCategoryPoints(eligibleResults, SuperSeniorSideCut)
                : 0;
            var womanOfTheYearPoints = isWoman
                ? SumCategoryPoints(eligibleResults, WomanSideCut)
                : 0;
            var youthOfTheYearPoints = isYouth
                ? bowlerResults.Sum(r => r.Points)
                : 0;

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
                TotalTournaments: totalTournamentIds.Count,
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

    // A category's points come from the bowler's category-specific side-cut result where one
    // exists for a tournament (a bowler placed both in the main field and their side cut), falling
    // back to the main-field result's points for tournaments with no side-cut row for them at all -
    // matching the "side-cut handling" the plan describes for the four restricted-award categories.
    private static int SumCategoryPoints(IReadOnlyCollection<LegacyBowlerResultRow> eligibleResults, int sideCut)
        => eligibleResults
            .GroupBy(r => r.TournamentId)
            .Sum(g => g.SingleOrDefault(r => r.SideCut == sideCut)?.Points ?? g.First(r => r.SideCut is null or 999).Points);

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
    // bowler's Eligible/Total tournament and entry counts (see the plan's field-by-field mapping).
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
