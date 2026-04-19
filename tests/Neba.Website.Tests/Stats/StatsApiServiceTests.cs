using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Stats;
using Neba.Api.Contracts.Stats.GetSeasonStats;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Stats;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;
using Neba.Website.Server.Stats;

using Refit;

namespace Neba.Website.Tests.Stats;

[UnitTest]
[Component("Website.Stats.StatsApiService")]
public sealed class StatsApiServiceTests
{
    private const string BowlerId = "01JX0000011111111111111111";

    private readonly Mock<IStatsApi> _mockStatsApi;
    private readonly StatsApiService _service;

    public StatsApiServiceTests()
    {
        _mockStatsApi = new Mock<IStatsApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        var executor = new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance);
        _service = new StatsApiService(executor, _mockStatsApi.Object);
    }

    // ── GetStatsAsync ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetStatsAsync should return mapped view model when API succeeds")]
    public async Task GetStatsAsync_ShouldReturnMappedViewModel_WhenApiSucceeds()
    {
        var response = GetSeasonStatsResponseFactory.Create();
        SetupSuccess(response);

        var result = await _service.GetStatsAsync(ct: TestContext.Current.CancellationToken);

        result.SelectedSeason.ShouldBe(response.SelectedSeason);
        result.AvailableSeasons.ShouldBe(response.AvailableSeasons);
        result.MinimumNumberOfGames.ShouldBe(response.MinimumNumberOfGames);
        result.MinimumNumberOfTournaments.ShouldBe(response.MinimumNumberOfTournaments);
        result.MinimumNumberOfEntries.ShouldBe(response.MinimumNumberOfEntries);
    }

    [Fact(DisplayName = "GetStatsAsync should return empty view model when API fails")]
    public async Task GetStatsAsync_ShouldReturnEmptyViewModel_WhenApiFails()
    {
        SetupFailure();

        var result = await _service.GetStatsAsync(ct: TestContext.Current.CancellationToken);

        result.SelectedSeason.ShouldBe("");
        result.BowlerOfTheYear.ShouldBeEmpty();
        result.HighAverage.ShouldBeEmpty();
        result.AllBowlers.ShouldBeEmpty();
    }

    // ── Rank = i + 1 for every leaderboard ────────────────────────────────────

    [Fact(DisplayName = "GetStatsAsync should assign rank 1 to the first item in each leaderboard")]
    public async Task GetStatsAsync_ShouldAssignRankOne_ToFirstItemInEachLeaderboard()
    {
        var response = GetSeasonStatsResponseFactory.Create(
            bowlerOfTheYear: [BowlerOfTheYearStandingResponseFactory.Create()],
            highAverage: [HighAverageResponseFactory.Create()],
            highBlock: [HighBlockResponseFactory.Create()],
            matchPlayAverage: [MatchPlayAverageResponseFactory.Create()],
            matchPlayRecord: [MatchPlayRecordResponseFactory.Create()],
            matchPlayAppearances: [MatchPlayAppearancesResponseFactory.Create()],
            pointsPerEntry: [PointsPerEntryResponseFactory.Create()],
            pointsPerTournament: [PointsPerTournamentResponseFactory.Create()],
            finalsPerEntry: [FinalsPerEntryResponseFactory.Create()],
            averageFinishes: [AverageFinishResponseFactory.Create()],
            allBowlers: [FullStatModalRowResponseFactory.Create()]);
        SetupSuccess(response);

        var result = await _service.GetStatsAsync(ct: TestContext.Current.CancellationToken);

        result.BowlerOfTheYear.First().Rank.ShouldBe(1);
        result.HighAverage.First().Rank.ShouldBe(1);
        result.HighBlock.First().Rank.ShouldBe(1);
        result.MatchPlayAverage.First().Rank.ShouldBe(1);
        result.MatchPlayRecord.First().Rank.ShouldBe(1);
        result.MatchPlayAppearances.First().Rank.ShouldBe(1);
        result.PointsPerEntry.First().Rank.ShouldBe(1);
        result.PointsPerTournament.First().Rank.ShouldBe(1);
        result.FinalsPerEntry.First().Rank.ShouldBe(1);
        result.AverageFinishes.First().Rank.ShouldBe(1);
        result.AllBowlers.First().Rank.ShouldBe(1);
    }

    [Fact(DisplayName = "GetStatsAsync should assign sequential ranks when multiple items present")]
    public async Task GetStatsAsync_ShouldAssignSequentialRanks_WhenMultipleItemsPresent()
    {
        var response = GetSeasonStatsResponseFactory.Create(
            highAverage: [HighAverageResponseFactory.Create(), HighAverageResponseFactory.Create()]);
        SetupSuccess(response);

        var result = await _service.GetStatsAsync(ct: TestContext.Current.CancellationToken);

        result.HighAverage.First().Rank.ShouldBe(1);
        result.HighAverage.Last().Rank.ShouldBe(2);
    }

    // ── MatchPlayAverage == 0 → null in AllBowlers ─────────────────────────────

    [Fact(DisplayName = "GetStatsAsync should map zero MatchPlayAverage as null in AllBowlers")]
    public async Task GetStatsAsync_ShouldMapZeroMatchPlayAverage_AsNullInAllBowlers()
    {
        var response = GetSeasonStatsResponseFactory.Create(
            allBowlers: [FullStatModalRowResponseFactory.Create(matchPlayAverage: 0)]);
        SetupSuccess(response);

        var result = await _service.GetStatsAsync(ct: TestContext.Current.CancellationToken);

        result.AllBowlers.First().MatchPlayAverage.ShouldBeNull();
    }

    [Fact(DisplayName = "GetStatsAsync should preserve non-zero MatchPlayAverage in AllBowlers")]
    public async Task GetStatsAsync_ShouldPreserveNonZeroMatchPlayAverage_InAllBowlers()
    {
        var response = GetSeasonStatsResponseFactory.Create(
            allBowlers: [FullStatModalRowResponseFactory.Create(matchPlayAverage: 215.5m)]);
        SetupSuccess(response);

        var result = await _service.GetStatsAsync(ct: TestContext.Current.CancellationToken);

        result.AllBowlers.First().MatchPlayAverage.ShouldBe(215.5m);
    }

    // ── FullStatModalRowViewModel.WinPercentage computed property ──────────────

    [Fact(DisplayName = "GetStatsAsync should return null WinPercentage when bowler has no match play games")]
    public async Task GetStatsAsync_ShouldReturnNullWinPercentage_WhenNoMatchPlayGames()
    {
        var response = GetSeasonStatsResponseFactory.Create(
            allBowlers: [FullStatModalRowResponseFactory.Create(wins: 0, losses: 0)]);
        SetupSuccess(response);

        var result = await _service.GetStatsAsync(ct: TestContext.Current.CancellationToken);

        result.AllBowlers.First().WinPercentage.ShouldBeNull();
    }

    [Fact(DisplayName = "GetStatsAsync should calculate correct WinPercentage from wins and losses")]
    public async Task GetStatsAsync_ShouldCalculateCorrectWinPercentage()
    {
        var response = GetSeasonStatsResponseFactory.Create(
            allBowlers: [FullStatModalRowResponseFactory.Create(wins: 9, losses: 6)]);
        SetupSuccess(response);

        var result = await _service.GetStatsAsync(ct: TestContext.Current.CancellationToken);

        result.AllBowlers.First().WinPercentage.ShouldBe(60.0m);
    }

    // ── Field mappings in GetStatsAsync ────────────────────────────────────────

    [Fact(DisplayName = "GetStatsAsync should map all response fields to the view model")]
    public async Task GetStatsAsync_ShouldMapAllResponseFields()
    {
        var bowlerResponse = FullStatModalRowResponseFactory.Create(
            bowlerId: BowlerId,
            points: 300,
            average: 220.5m,
            games: 56,
            finals: 4,
            wins: 12,
            losses: 8,
            winnings: 1200m,
            fieldAverage: 15.3m,
            tournaments: 9);
        var response = GetSeasonStatsResponseFactory.Create(allBowlers: [bowlerResponse]);
        SetupSuccess(response);

        var result = await _service.GetStatsAsync(ct: TestContext.Current.CancellationToken);
        var row = result.AllBowlers.First();

        row.BowlerId.ShouldBe(BowlerId);
        row.Points.ShouldBe(300);
        row.Average.ShouldBe(220.5m);
        row.Games.ShouldBe(56);
        row.Finals.ShouldBe(4);
        row.Wins.ShouldBe(12);
        row.Loses.ShouldBe(8);
        row.Winnings.ShouldBe(1200m);
        row.FieldAverage.ShouldBe(15.3m);
        row.Tournaments.ShouldBe(9);
    }

    // ── GetIndividualStatsAsync ────────────────────────────────────────────────

    [Fact(DisplayName = "GetIndividualStatsAsync should return null when API fails")]
    public async Task GetIndividualStatsAsync_ShouldReturnNull_WhenApiFails()
    {
        SetupFailure();

        var result = await _service.GetIndividualStatsAsync(BowlerId, ct: TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact(DisplayName = "GetIndividualStatsAsync should return null when bowler is not in AllBowlers")]
    public async Task GetIndividualStatsAsync_ShouldReturnNull_WhenBowlerNotFound()
    {
        var response = GetSeasonStatsResponseFactory.Create(
            allBowlers: [FullStatModalRowResponseFactory.Create(bowlerId: "different-id")]);
        SetupSuccess(response);

        var result = await _service.GetIndividualStatsAsync(BowlerId, ct: TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact(DisplayName = "GetIndividualStatsAsync should return mapped view model when bowler is found")]
    public async Task GetIndividualStatsAsync_ShouldReturnMappedViewModel_WhenBowlerFound()
    {
        var response = GetSeasonStatsResponseFactory.Create(
            allBowlers: [FullStatModalRowResponseFactory.Create(bowlerId: BowlerId)]);
        SetupSuccess(response);

        var result = await _service.GetIndividualStatsAsync(BowlerId, ct: TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.BowlerId.ShouldBe(BowlerId);
        result.SelectedSeason.ShouldBe(response.SelectedSeason);
    }

    // ── FindRank ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetIndividualStatsAsync should return rank 1 when bowler is first in a leaderboard")]
    public async Task GetIndividualStatsAsync_ShouldReturnRankOne_WhenBowlerIsFirstInLeaderboard()
    {
        var response = GetSeasonStatsResponseFactory.Create(
            bowlerOfTheYear: [BowlerOfTheYearStandingResponseFactory.Create(bowlerId: BowlerId)],
            highAverage: [HighAverageResponseFactory.Create(bowlerId: BowlerId)],
            allBowlers: [FullStatModalRowResponseFactory.Create(bowlerId: BowlerId)]);
        SetupSuccess(response);

        var result = await _service.GetIndividualStatsAsync(BowlerId, ct: TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.BowlerOfTheYearRank.ShouldBe(1);
        result.HighAverageRank.ShouldBe(1);
    }

    [Fact(DisplayName = "GetIndividualStatsAsync should return rank 2 when bowler is second in a leaderboard")]
    public async Task GetIndividualStatsAsync_ShouldReturnRankTwo_WhenBowlerIsSecond()
    {
        var response = GetSeasonStatsResponseFactory.Create(
            bowlerOfTheYear:
            [
                BowlerOfTheYearStandingResponseFactory.Create(bowlerId: "other-bowler"),
                BowlerOfTheYearStandingResponseFactory.Create(bowlerId: BowlerId)
            ],
            allBowlers: [FullStatModalRowResponseFactory.Create(bowlerId: BowlerId)]);
        SetupSuccess(response);

        var result = await _service.GetIndividualStatsAsync(BowlerId, ct: TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.BowlerOfTheYearRank.ShouldBe(2);
    }

    [Fact(DisplayName = "GetIndividualStatsAsync should return null rank when bowler is absent from a leaderboard")]
    public async Task GetIndividualStatsAsync_ShouldReturnNullRank_WhenBowlerAbsentFromLeaderboard()
    {
        var response = GetSeasonStatsResponseFactory.Create(
            bowlerOfTheYear: [BowlerOfTheYearStandingResponseFactory.Create(bowlerId: "other-bowler")],
            allBowlers: [FullStatModalRowResponseFactory.Create(bowlerId: BowlerId)]);
        SetupSuccess(response);

        var result = await _service.GetIndividualStatsAsync(BowlerId, ct: TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.BowlerOfTheYearRank.ShouldBeNull();
    }

    // ── entriesFromStanding ?? full.Tournaments ────────────────────────────────

    [Fact(DisplayName = "GetIndividualStatsAsync should use Entries from BOTY standing when bowler is ranked")]
    public async Task GetIndividualStatsAsync_ShouldUseEntriesFromStanding_WhenBowlerIsRanked()
    {
        const int standingEntries = 14;
        const int allBowlerTournaments = 9;
        var response = GetSeasonStatsResponseFactory.Create(
            bowlerOfTheYear: [BowlerOfTheYearStandingResponseFactory.Create(bowlerId: BowlerId, entries: standingEntries)],
            allBowlers: [FullStatModalRowResponseFactory.Create(bowlerId: BowlerId, tournaments: allBowlerTournaments)]);
        SetupSuccess(response);

        var result = await _service.GetIndividualStatsAsync(BowlerId, ct: TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Entries.ShouldBe(standingEntries);
    }

    [Fact(DisplayName = "GetIndividualStatsAsync should fall back to Tournaments when bowler has no standing")]
    public async Task GetIndividualStatsAsync_ShouldFallBackToTournaments_WhenBowlerHasNoStanding()
    {
        const int allBowlerTournaments = 9;
        var response = GetSeasonStatsResponseFactory.Create(
            bowlerOfTheYear: [],
            seniorOfTheYear: [],
            superSeniorOfTheYear: [],
            womanOfTheYear: [],
            rookieOfTheYear: [],
            youthOfTheYear: [],
            allBowlers: [FullStatModalRowResponseFactory.Create(bowlerId: BowlerId, tournaments: allBowlerTournaments)]);
        SetupSuccess(response);

        var result = await _service.GetIndividualStatsAsync(BowlerId, ct: TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Entries.ShouldBe(allBowlerTournaments);
    }

    // ── MatchPlayAverage == 0 → null in individual stats ──────────────────────

    [Fact(DisplayName = "GetIndividualStatsAsync should map zero MatchPlayAverage as null")]
    public async Task GetIndividualStatsAsync_ShouldMapZeroMatchPlayAverage_AsNull()
    {
        var response = GetSeasonStatsResponseFactory.Create(
            allBowlers: [FullStatModalRowResponseFactory.Create(bowlerId: BowlerId, matchPlayAverage: 0)]);
        SetupSuccess(response);

        var result = await _service.GetIndividualStatsAsync(BowlerId, ct: TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.MatchPlayAverage.ShouldBeNull();
    }

    [Fact(DisplayName = "GetIndividualStatsAsync should preserve non-zero MatchPlayAverage")]
    public async Task GetIndividualStatsAsync_ShouldPreserveNonZeroMatchPlayAverage()
    {
        var response = GetSeasonStatsResponseFactory.Create(
            allBowlers: [FullStatModalRowResponseFactory.Create(bowlerId: BowlerId, matchPlayAverage: 219.3m)]);
        SetupSuccess(response);

        var result = await _service.GetIndividualStatsAsync(BowlerId, ct: TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.MatchPlayAverage.ShouldBe(219.3m);
    }

    // ── IndividualStatsPageViewModel.WinPercentage ─────────────────────────────

    [Fact(DisplayName = "GetIndividualStatsAsync should return null WinPercentage when bowler has no match play games")]
    public async Task GetIndividualStatsAsync_ShouldReturnNullWinPercentage_WhenNoMatchPlayGames()
    {
        var response = GetSeasonStatsResponseFactory.Create(
            allBowlers: [FullStatModalRowResponseFactory.Create(bowlerId: BowlerId, wins: 0, losses: 0)]);
        SetupSuccess(response);

        var result = await _service.GetIndividualStatsAsync(BowlerId, ct: TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.WinPercentage.ShouldBeNull();
    }

    [Fact(DisplayName = "GetIndividualStatsAsync should calculate correct WinPercentage")]
    public async Task GetIndividualStatsAsync_ShouldCalculateCorrectWinPercentage()
    {
        var response = GetSeasonStatsResponseFactory.Create(
            allBowlers: [FullStatModalRowResponseFactory.Create(bowlerId: BowlerId, wins: 9, losses: 6)]);
        SetupSuccess(response);

        var result = await _service.GetIndividualStatsAsync(BowlerId, ct: TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.WinPercentage.ShouldBe(60.0m);
    }

    // ── Points race filter by bowlerId ─────────────────────────────────────────

    [Fact(DisplayName = "GetIndividualStatsAsync should include only the bowler's points race series")]
    public async Task GetIndividualStatsAsync_ShouldFilterPointsRace_ToBowler()
    {
        var myRace = PointsRaceSeriesResponseFactory.Create(bowlerId: BowlerId);
        var otherRace = PointsRaceSeriesResponseFactory.Create(bowlerId: "other-bowler");
        var response = GetSeasonStatsResponseFactory.Create(
            bowlerOfTheYearPointsRace: [myRace, otherRace],
            allBowlers: [FullStatModalRowResponseFactory.Create(bowlerId: BowlerId)]);
        SetupSuccess(response);

        var result = await _service.GetIndividualStatsAsync(BowlerId, ct: TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.BowlerOfTheYearPointsRace.ShouldNotBeNull();
        result.BowlerOfTheYearPointsRace!.BowlerId.ShouldBe(BowlerId);
    }

    [Fact(DisplayName = "GetIndividualStatsAsync should return null points race when bowler has no race data")]
    public async Task GetIndividualStatsAsync_ShouldReturnNullPointsRace_WhenNoneExist()
    {
        var response = GetSeasonStatsResponseFactory.Create(
            bowlerOfTheYearPointsRace: [PointsRaceSeriesResponseFactory.Create(bowlerId: "other-bowler")],
            allBowlers: [FullStatModalRowResponseFactory.Create(bowlerId: BowlerId)]);
        SetupSuccess(response);

        var result = await _service.GetIndividualStatsAsync(BowlerId, ct: TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.BowlerOfTheYearPointsRace.ShouldBeNull();
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private void SetupSuccess(GetSeasonStatsResponse response)
    {
        var apiResponse = new Mock<IApiResponse<GetSeasonStatsResponse>>(MockBehavior.Strict);
        apiResponse.Setup(r => r.IsSuccessStatusCode).Returns(true);
        apiResponse.Setup(r => r.Content).Returns(response);
        apiResponse.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);

        _mockStatsApi
            .Setup(x => x.GetSeasonStatsAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse.Object);
    }

    private void SetupFailure()
    {
        var apiResponse = new Mock<IApiResponse<GetSeasonStatsResponse>>(MockBehavior.Strict);
        apiResponse.Setup(r => r.IsSuccessStatusCode).Returns(false);
        apiResponse.Setup(r => r.Content).Returns((GetSeasonStatsResponse?)null);
        apiResponse.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.InternalServerError);

        _mockStatsApi
            .Setup(x => x.GetSeasonStatsAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse.Object);
    }
}
