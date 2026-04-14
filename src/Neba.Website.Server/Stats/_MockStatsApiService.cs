namespace Neba.Website.Server.Stats;

#pragma warning disable

internal sealed class MockStatsApiService : IStatsApiService
{
    public Task<StatsPageViewModel> GetStatsAsync(Ulid? seasonId = null, CancellationToken ct = default)
        => Task.FromResult(MockStatsData.CurrentSeasonStats());

    public Task<IndividualStatsPageViewModel?> GetIndividualStatsAsync(Ulid bowlerId, Ulid? seasonId = null, CancellationToken ct = default)
    {
        var stats = MockStatsData.CurrentSeasonStats();

        // Find primary stats row; fall back to top-ranked BOY for demo purposes
        var full = stats.AllBowlers.FirstOrDefault(r => r.BowlerId == bowlerId)
            ?? stats.AllBowlers.FirstOrDefault();

        if (full is null)
            return Task.FromResult<IndividualStatsPageViewModel?>(null);

        var id = full.BowlerId;
        var boyRow = stats.BowlerOfTheYear.FirstOrDefault(r => r.BowlerId == id);

        var model = new IndividualStatsPageViewModel
        {
            BowlerId = id,
            BowlerName = full.BowlerName,
            SelectedSeason = stats.SelectedSeason,
            AvailableSeasons = stats.AvailableSeasons,

            Points = full.Points,
            Average = full.Average,
            Games = full.Games,
            Finals = full.Finals,
            Entries = boyRow?.Entries ?? full.Touranments,
            Tournaments = full.Touranments,
            Winnings = full.Winnings,
            FieldAverage = full.FieldAverage,
            MatchPlayWins = full.Wins,
            MatchPlayLosses = full.Loses,
            MatchPlayAverage = full.MatchPlayAverage,

            BowlerOfTheYearRank = stats.BowlerOfTheYear.FirstOrDefault(r => r.BowlerId == id)?.Rank,
            SeniorOfTheYearRank = stats.SeniorOfTheYear.FirstOrDefault(r => r.BowlerId == id)?.Rank,
            SuperSeniorOfTheYearRank = stats.SuperSeniorOfTheYear.FirstOrDefault(r => r.BowlerId == id)?.Rank,
            WomanOfTheYearRank = stats.WomanOfTheYear.FirstOrDefault(r => r.BowlerId == id)?.Rank,
            RookieOfTheYearRank = stats.RookieOfTheYear.FirstOrDefault(r => r.BowlerId == id)?.Rank,
            YouthOfTheYearRank = stats.YouthOfTheYear.FirstOrDefault(r => r.BowlerId == id)?.Rank,

            HighAverageRank = stats.HighAverage.FirstOrDefault(r => r.BowlerId == id)?.Rank,
            HighBlockRank = stats.HighBlock.FirstOrDefault(r => r.BowlerId == id)?.Rank,
            MatchPlayAverageRank = stats.MatchPlayAverage.FirstOrDefault(r => r.BowlerId == id)?.Rank,

            MatchPlayRecordRank = stats.MatchPlayRecord.FirstOrDefault(r => r.BowlerId == id)?.Rank,
            MatchPlayAppearancesRank = stats.MatchPlayAppearances.FirstOrDefault(r => r.BowlerId == id)?.Rank,

            PointsPerEntryRank = stats.PointsPerEntry.FirstOrDefault(r => r.BowlerId == id)?.Rank,
            PointsPerTournamentRank = stats.PointsPerTournament.FirstOrDefault(r => r.BowlerId == id)?.Rank,
            FinalsPerEntryRank = stats.FinalsPerEntry.FirstOrDefault(r => r.BowlerId == id)?.Rank,
            AverageFinishRank = stats.AverageFinishes.FirstOrDefault(r => r.BowlerId == id)?.Rank,

            BowlerOfTheYearPointsRace = stats.BowlerOfTheYearPointsRace.FirstOrDefault(r => r.BowlerId == id),
        };

        return Task.FromResult<IndividualStatsPageViewModel?>(model);
    }
}
