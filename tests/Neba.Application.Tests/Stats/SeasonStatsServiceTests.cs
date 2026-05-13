using System.Text.Json;

using Ardalis.SmartEnum.SystemTextJson;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Application.Caching;
using Neba.Application.Stats;
using Neba.Application.Tournaments;
using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Stats;

namespace Neba.Application.Tests.Stats;

[UnitTest]
[Component("Stats")]
public sealed class SeasonStatsServiceTests
{
    private readonly Mock<IStatsQueries> _statsQueriesMock;
    private readonly Mock<ITournamentQueries> _tournamentQueriesMock;
    private readonly FakeLogger<SeasonStatsService> _logger;
    private readonly SeasonStatsService _service;

    public SeasonStatsServiceTests()
    {
        _statsQueriesMock = new Mock<IStatsQueries>(MockBehavior.Strict);
        _tournamentQueriesMock = new Mock<ITournamentQueries>(MockBehavior.Strict);
        _logger = new FakeLogger<SeasonStatsService>();

        var services = new ServiceCollection();
        services.AddHybridCache();
        services.AddKeyedSingleton<JsonSerializerOptions>(
            typeof(IHybridCacheSerializer<>),
            new JsonSerializerOptions
            {
                Converters = { new SmartEnumNameConverter<NameSuffix, string>() }
            });
        var cache = services.BuildServiceProvider().GetRequiredService<HybridCache>();

        _service = new SeasonStatsService(
            _statsQueriesMock.Object, _tournamentQueriesMock.Object, cache, _logger);
    }

    [Fact(DisplayName = "GetSeasonsWithStatsAsync should return seasons from query on cache miss")]
    public async Task GetSeasonsWithStatsAsync_ShouldReturnSeasonsFromQuery_OnCacheMiss()
    {
        // Arrange
        var expectedSeasons = SeasonDtoFactory.Bogus(3);
        _statsQueriesMock
            .Setup(x => x.GetSeasonsWithStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSeasons);

        // Act
        var result = await _service.GetSeasonsWithStatsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(expectedSeasons);
    }

    [Fact(DisplayName = "GetSeasonsWithStatsAsync should not call query on second invocation when cache is warm")]
    public async Task GetSeasonsWithStatsAsync_ShouldNotCallQuery_OnCacheHit()
    {
        // Arrange
        var seasons = SeasonDtoFactory.Bogus(3);
        var callCount = 0;
        _statsQueriesMock
            .Setup(x => x.GetSeasonsWithStatsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callCount++)
            .ReturnsAsync(seasons);

        // Act
        await _service.GetSeasonsWithStatsAsync(TestContext.Current.CancellationToken);
        await _service.GetSeasonsWithStatsAsync(TestContext.Current.CancellationToken);

        // Assert
        callCount.ShouldBe(1, "query should only be invoked once; second call should be served from cache");
    }

    [Fact(DisplayName = "GetSeasonsWithStatsAsync should log cache miss with the correct cache key")]
    public async Task GetSeasonsWithStatsAsync_ShouldLogCacheMiss_WithCorrectKey()
    {
        // Arrange
        _statsQueriesMock
            .Setup(x => x.GetSeasonsWithStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(SeasonDtoFactory.Bogus(1));

        // Act
        await _service.GetSeasonsWithStatsAsync(TestContext.Current.CancellationToken);

        // Assert
        var logs = _logger.Collector.GetSnapshot();
        logs.ShouldContain(l =>
            l.Level == LogLevel.Information &&
            l.Message.Contains(CacheDescriptors.Stats.ListSeasonsWithStats.Key));
    }

    [Fact(DisplayName = "GetBowlerSeasonStatsAsync should return stats from query on cache miss")]
    public async Task GetBowlerSeasonStatsAsync_ShouldReturnStatsFromQuery_OnCacheMiss()
    {
        // Arrange
        var seasonId = SeasonId.New();
        var expectedStats = BowlerSeasonStatsDtoFactory.Bogus(5);
        _statsQueriesMock
            .Setup(x => x.GetBowlerSeasonStatsAsync(seasonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _service.GetBowlerSeasonStatsAsync(seasonId, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(expectedStats);
    }

    [Fact(DisplayName = "GetBowlerSeasonStatsAsync should not call query on second invocation when cache is warm")]
    public async Task GetBowlerSeasonStatsAsync_ShouldNotCallQuery_OnCacheHit()
    {
        // Arrange
        var seasonId = SeasonId.New();
        var stats = BowlerSeasonStatsDtoFactory.Bogus(5);
        var callCount = 0;
        _statsQueriesMock
            .Setup(x => x.GetBowlerSeasonStatsAsync(seasonId, It.IsAny<CancellationToken>()))
            .Callback(() => callCount++)
            .ReturnsAsync(stats);

        // Act
        await _service.GetBowlerSeasonStatsAsync(seasonId, TestContext.Current.CancellationToken);
        await _service.GetBowlerSeasonStatsAsync(seasonId, TestContext.Current.CancellationToken);

        // Assert
        callCount.ShouldBe(1, "query should only be invoked once; second call should be served from cache");
    }

    [Fact(DisplayName = "GetBowlerSeasonStatsAsync should log cache miss with the correct cache key")]
    public async Task GetBowlerSeasonStatsAsync_ShouldLogCacheMiss_WithCorrectKey()
    {
        // Arrange
        var seasonId = SeasonId.New();
        _statsQueriesMock
            .Setup(x => x.GetBowlerSeasonStatsAsync(seasonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BowlerSeasonStatsDtoFactory.Bogus(1));

        // Act
        await _service.GetBowlerSeasonStatsAsync(seasonId, TestContext.Current.CancellationToken);

        // Assert
        var logs = _logger.Collector.GetSnapshot();
        logs.ShouldContain(l =>
            l.Level == LogLevel.Information &&
            l.Message.Contains(CacheDescriptors.Stats.BowlerSeasonStats(seasonId).Key));
    }

    [Fact(DisplayName = "GetBowlerSeasonStatsAsync should cache results independently per season")]
    public async Task GetBowlerSeasonStatsAsync_ShouldCacheIndependently_PerSeason()
    {
        // Arrange
        var seasonId1 = SeasonId.New();
        var seasonId2 = SeasonId.New();
        var callCount = 0;
        _statsQueriesMock
            .Setup(x => x.GetBowlerSeasonStatsAsync(seasonId1, It.IsAny<CancellationToken>()))
            .Callback(() => callCount++)
            .ReturnsAsync(BowlerSeasonStatsDtoFactory.Bogus(3));
        _statsQueriesMock
            .Setup(x => x.GetBowlerSeasonStatsAsync(seasonId2, It.IsAny<CancellationToken>()))
            .Callback(() => callCount++)
            .ReturnsAsync(BowlerSeasonStatsDtoFactory.Bogus(3));

        // Act
        await _service.GetBowlerSeasonStatsAsync(seasonId1, TestContext.Current.CancellationToken);
        await _service.GetBowlerSeasonStatsAsync(seasonId2, TestContext.Current.CancellationToken);

        // Assert
        callCount.ShouldBe(2, "each season should have its own cache entry and require a separate query");
    }

    // CalculateSeasonStatsSummary — Season At A Glance

    [Fact(DisplayName = "CalculateSeasonStatsSummary should sum TotalEntries across all bowlers")]
    public void CalculateSeasonStatsSummary_ShouldSumTotalEntries()
    {
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(totalEntries: 10),
            BowlerSeasonStatsDtoFactory.Create(totalEntries: 15),
            BowlerSeasonStatsDtoFactory.Create(totalEntries: 5),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.TotalEntries.ShouldBe(30);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should sum TournamentWinnings as TotalPrizeMoney")]
    public void CalculateSeasonStatsSummary_ShouldSumTournamentWinningsAsTotalPrizeMoney()
    {
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(tournamentWinnings: 500m),
            BowlerSeasonStatsDtoFactory.Create(tournamentWinnings: 1250m),
            BowlerSeasonStatsDtoFactory.Create(tournamentWinnings: 750m),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.TotalPrizeMoney.ShouldBe(2500m);
    }

    // CalculateSeasonStatsSummary — Season Bests

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return the highest qualifying game and include all tied bowlers")]
    public void CalculateSeasonStatsSummary_ShouldReturnHighGameFromQualifyingAndBowlers()
    {
        var bowler1Id = BowlerId.New();
        var bowler1Name = NameFactory.Create();
        var bowler2Id = BowlerId.New();
        var bowler2Name = NameFactory.Create();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: bowler1Id, bowlerName: bowler1Name, qualifyingHighGame: 299, matchPlayHighGame: 240),
            BowlerSeasonStatsDtoFactory.Create(bowlerId: bowler2Id, bowlerName: bowler2Name, qualifyingHighGame: 299, matchPlayHighGame: 250),
            BowlerSeasonStatsDtoFactory.Create(qualifyingHighGame: 270, matchPlayHighGame: 280),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.HighGame.ShouldBe(299);
        result.HighGameBowlers.Count.ShouldBe(2);
        result.HighGameBowlers.ShouldContainKeyAndValue(bowler1Id, bowler1Name);
        result.HighGameBowlers.ShouldContainKeyAndValue(bowler2Id, bowler2Name);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return the highest match play game when it exceeds qualifying and include all tied bowlers")]
    public void CalculateSeasonStatsSummary_ShouldReturnHighGameFromMatchPlayAndBowlers()
    {
        var bowler1Id = BowlerId.New();
        var bowler1Name = NameFactory.Create();
        var bowler2Id = BowlerId.New();
        var bowler2Name = NameFactory.Create();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: bowler1Id, bowlerName: bowler1Name, qualifyingHighGame: 260, matchPlayHighGame: 299),
            BowlerSeasonStatsDtoFactory.Create(bowlerId: bowler2Id, bowlerName: bowler2Name, qualifyingHighGame: 270, matchPlayHighGame: 299),
            BowlerSeasonStatsDtoFactory.Create(qualifyingHighGame: 280, matchPlayHighGame: 265),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.HighGame.ShouldBe(299);
        result.HighGameBowlers.Count.ShouldBe(2);
        result.HighGameBowlers.ShouldContainKeyAndValue(bowler1Id, bowler1Name);
        result.HighGameBowlers.ShouldContainKeyAndValue(bowler2Id, bowler2Name);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return the highest block and include all tied bowlers")]
    public void CalculateSeasonStatsSummary_ShouldReturnHighBlockAndBowlers()
    {
        var bowler1Id = BowlerId.New();
        var bowler1Name = NameFactory.Create();
        var bowler2Id = BowlerId.New();
        var bowler2Name = NameFactory.Create();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: bowler1Id, bowlerName: bowler1Name, highBlock: 1380),
            BowlerSeasonStatsDtoFactory.Create(bowlerId: bowler2Id, bowlerName: bowler2Name, highBlock: 1380),
            BowlerSeasonStatsDtoFactory.Create(highBlock: 1250),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.HighBlock.ShouldBe(1380);
        result.HighBlockBowlers.Count.ShouldBe(2);
        result.HighBlockBowlers.ShouldContainKeyAndValue(bowler1Id, bowler1Name);
        result.HighBlockBowlers.ShouldContainKeyAndValue(bowler2Id, bowler2Name);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return the highest average and include all tied bowlers")]
    public void CalculateSeasonStatsSummary_ShouldReturnHighAverageAndBowlers()
    {
        // 24000 * 1m / 80 = 300; 22800 * 1m / 76 = 300; 20000 * 1m / 100 = 200
        var bowler1Id = BowlerId.New();
        var bowler1Name = NameFactory.Create();
        var bowler2Id = BowlerId.New();
        var bowler2Name = NameFactory.Create();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: bowler1Id, bowlerName: bowler1Name, totalGames: 80, totalPinfall: 24000),
            BowlerSeasonStatsDtoFactory.Create(bowlerId: bowler2Id, bowlerName: bowler2Name, totalGames: 76, totalPinfall: 22800),
            BowlerSeasonStatsDtoFactory.Create(totalGames: 100, totalPinfall: 20000),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.HighAverage.ShouldBe(300m);
        result.HighAverageBowlers.Count.ShouldBe(2);
        result.HighAverageBowlers.ShouldContainKeyAndValue(bowler1Id, bowler1Name);
        result.HighAverageBowlers.ShouldContainKeyAndValue(bowler2Id, bowler2Name);
    }

    // CalculateSeasonStatsSummary — Field Match Play Summary

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return the highest match play win percentage and include all tied bowlers")]
    public void CalculateSeasonStatsSummary_ShouldReturnHighestMatchPlayWinPercentageAndBowlers()
    {
        // 8 * 1m / 10 = 80.0%; 4 * 1m / 5 = 80.0%; 6 * 1m / 10 = 60.0%; 0 wins+losses excluded
        var bowler1Id = BowlerId.New();
        var bowler1Name = NameFactory.Create();
        var bowler2Id = BowlerId.New();
        var bowler2Name = NameFactory.Create();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: bowler1Id, bowlerName: bowler1Name, matchPlayWins: 8, matchPlayLosses: 2),
            BowlerSeasonStatsDtoFactory.Create(bowlerId: bowler2Id, bowlerName: bowler2Name, matchPlayWins: 4, matchPlayLosses: 1),
            BowlerSeasonStatsDtoFactory.Create(matchPlayWins: 6, matchPlayLosses: 4),
            BowlerSeasonStatsDtoFactory.Create(matchPlayWins: 0, matchPlayLosses: 0),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.HighestMatchPlayWinPercentage.ShouldBe(80.0m);
        result.HighestMatchPlayWinPercentageBowlers.Count.ShouldBe(2);
        result.HighestMatchPlayWinPercentageBowlers.ShouldContainKeyAndValue(bowler1Id, bowler1Name);
        result.HighestMatchPlayWinPercentageBowlers.ShouldContainKeyAndValue(bowler2Id, bowler2Name);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return the most finals and include all tied bowlers")]
    public void CalculateSeasonStatsSummary_ShouldReturnMostFinalsAndBowlers()
    {
        var bowler1Id = BowlerId.New();
        var bowler1Name = NameFactory.Create();
        var bowler2Id = BowlerId.New();
        var bowler2Name = NameFactory.Create();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: bowler1Id, bowlerName: bowler1Name, finals: 9),
            BowlerSeasonStatsDtoFactory.Create(bowlerId: bowler2Id, bowlerName: bowler2Name, finals: 9),
            BowlerSeasonStatsDtoFactory.Create(finals: 5),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.MostFinals.ShouldBe(9);
        result.MostFinalsBowlers.Count.ShouldBe(2);
        result.MostFinalsBowlers.ShouldContainKeyAndValue(bowler1Id, bowler1Name);
        result.MostFinalsBowlers.ShouldContainKeyAndValue(bowler2Id, bowler2Name);
    }

    // CalculateSeasonStatsSummary — Award Standings

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return BowlerOfTheYear standings ordered by points descending with only bowlers who have points")]
    public void CalculateSeasonStatsSummary_ShouldReturnBowlerOfTheYearStandings()
    {
        var bowler1Id = BowlerId.New();
        var bowler2Id = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: bowler1Id, bowlerOfTheYearPoints: 300),
            BowlerSeasonStatsDtoFactory.Create(bowlerId: bowler2Id, bowlerOfTheYearPoints: 500),
            BowlerSeasonStatsDtoFactory.Create(bowlerOfTheYearPoints: 0),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.BowlerOfTheYear.Count.ShouldBe(2);
        result.BowlerOfTheYear.First().BowlerId.ShouldBe(bowler2Id);
        result.BowlerOfTheYear.Last().BowlerId.ShouldBe(bowler1Id);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return SeniorOfTheYear standings filtered to seniors only")]
    public void CalculateSeasonStatsSummary_ShouldReturnSeniorOfTheYearStandings_FilteredToSeniors()
    {
        var seniorId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: seniorId, isSenior: true, seniorOfTheYearPoints: 400),
            BowlerSeasonStatsDtoFactory.Create(isSenior: false, seniorOfTheYearPoints: 600),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.SeniorOfTheYear.Count.ShouldBe(1);
        result.SeniorOfTheYear.Single().BowlerId.ShouldBe(seniorId);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return SuperSeniorOfTheYear standings filtered to super seniors only")]
    public void CalculateSeasonStatsSummary_ShouldReturnSuperSeniorOfTheYearStandings_FilteredToSuperSeniors()
    {
        var superSeniorId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: superSeniorId, isSuperSenior: true, superSeniorOfTheYearPoints: 350),
            BowlerSeasonStatsDtoFactory.Create(isSuperSenior: false, superSeniorOfTheYearPoints: 700),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.SuperSeniorOfTheYear.Count.ShouldBe(1);
        result.SuperSeniorOfTheYear.Single().BowlerId.ShouldBe(superSeniorId);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return WomanOfTheYear standings filtered to women only")]
    public void CalculateSeasonStatsSummary_ShouldReturnWomanOfTheYearStandings_FilteredToWomen()
    {
        var womanId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: womanId, isWoman: true, womanOfTheYearPoints: 450),
            BowlerSeasonStatsDtoFactory.Create(isWoman: false, womanOfTheYearPoints: 800),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.WomanOfTheYear.Count.ShouldBe(1);
        result.WomanOfTheYear.Single().BowlerId.ShouldBe(womanId);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return RookieOfTheYear standings filtered to rookies using BowlerOfTheYear points")]
    public void CalculateSeasonStatsSummary_ShouldReturnRookieOfTheYearStandings_UsingBowlerOfTheYearPoints()
    {
        var rookieId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: rookieId, isRookie: true, bowlerOfTheYearPoints: 200),
            BowlerSeasonStatsDtoFactory.Create(isRookie: false, bowlerOfTheYearPoints: 500),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.RookieOfTheYear.Count.ShouldBe(1);
        result.RookieOfTheYear.Single().BowlerId.ShouldBe(rookieId);
        result.RookieOfTheYear.Single().Points.ShouldBe(200);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return YouthOfTheYear standings filtered to youth only")]
    public void CalculateSeasonStatsSummary_ShouldReturnYouthOfTheYearStandings_FilteredToYouth()
    {
        var youthId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: youthId, isYouth: true, youthOfTheYearPoints: 150),
            BowlerSeasonStatsDtoFactory.Create(isYouth: false, youthOfTheYearPoints: 300),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.YouthOfTheYear.Count.ShouldBe(1);
        result.YouthOfTheYear.Single().BowlerId.ShouldBe(youthId);
    }

    // CalculateSeasonStatsSummary — Bowler Search List

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return BowlerSearchList ordered by last name then first name")]
    public void CalculateSeasonStatsSummary_ShouldReturnBowlerSearchList_OrderedAlphabetically()
    {
        var abel = NameFactory.Create(firstName: "Aaron", lastName: "Abel");
        var baker1 = NameFactory.Create(firstName: "Alice", lastName: "Baker");
        var baker2 = NameFactory.Create(firstName: "Bob", lastName: "Baker");
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerName: baker2),
            BowlerSeasonStatsDtoFactory.Create(bowlerName: abel),
            BowlerSeasonStatsDtoFactory.Create(bowlerName: baker1),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.BowlerSearchList.Count.ShouldBe(3);
        var names = result.BowlerSearchList.Select(e => e.BowlerName).ToArray();
        names[0].ShouldBe(abel);
        names[1].ShouldBe(baker1);
        names[2].ShouldBe(baker2);
    }

    // CalculateSeasonStatsSummary — High Average Leaderboard

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return HighAverageLeaderboard ordered by average descending excluding bowlers with no games")]
    public void CalculateSeasonStatsSummary_ShouldReturnHighAverageLeaderboard_OrderedByAverageDescending()
    {
        var highId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: highId, totalGames: 40, totalPinfall: 8000),
            BowlerSeasonStatsDtoFactory.Create(totalGames: 60, totalPinfall: 10800),
            BowlerSeasonStatsDtoFactory.Create(totalGames: 0, totalPinfall: 0),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.HighAverageLeaderboard.Count.ShouldBe(2);
        result.HighAverageLeaderboard.First().BowlerId.ShouldBe(highId);
        result.HighAverageLeaderboard.First().Average.ShouldBe(200.00m);
        result.HighAverageLeaderboard.Last().Average.ShouldBe(180.00m);
    }

    // CalculateSeasonStatsSummary — High Block Leaderboard

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return HighBlockLeaderboard ordered by high block descending excluding bowlers with no block")]
    public void CalculateSeasonStatsSummary_ShouldReturnHighBlockLeaderboard_OrderedByHighBlockDescending()
    {
        var topId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: topId, highBlock: 1350),
            BowlerSeasonStatsDtoFactory.Create(highBlock: 1200),
            BowlerSeasonStatsDtoFactory.Create(highBlock: 0),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.HighBlockLeaderboard.Count.ShouldBe(2);
        result.HighBlockLeaderboard.First().BowlerId.ShouldBe(topId);
        result.HighBlockLeaderboard.First().HighBlock.ShouldBe(1350);
    }

    // CalculateSeasonStatsSummary — Match Play Average Leaderboard

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return MatchPlayAverageLeaderboard ordered by average descending excluding bowlers with no match play games")]
    public void CalculateSeasonStatsSummary_ShouldReturnMatchPlayAverageLeaderboard_OrderedByAverageDescending()
    {
        var topId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: topId, matchPlayGames: 4, matchPlayPinfall: 840),
            BowlerSeasonStatsDtoFactory.Create(matchPlayGames: 6, matchPlayPinfall: 1080),
            BowlerSeasonStatsDtoFactory.Create(matchPlayGames: 0, matchPlayPinfall: 0),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.MatchPlayAverageLeaderboard.Count.ShouldBe(2);
        result.MatchPlayAverageLeaderboard.First().BowlerId.ShouldBe(topId);
        result.MatchPlayAverageLeaderboard.First().MatchPlayAverage.ShouldBe(210.00m);
        result.MatchPlayAverageLeaderboard.Last().MatchPlayAverage.ShouldBe(180.00m);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should compute WinPercentage correctly in MatchPlayAverageLeaderboard")]
    public void CalculateSeasonStatsSummary_ShouldComputeWinPercentage_InMatchPlayAverageLeaderboard()
    {
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(matchPlayWins: 3, matchPlayLosses: 1, matchPlayGames: 4, matchPlayPinfall: 800),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.MatchPlayAverageLeaderboard.Single().WinPercentage.ShouldBe(75.00m);
    }

    // CalculateSeasonStatsSummary — Match Play Record Leaderboard

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return MatchPlayRecordLeaderboard ordered by win percentage descending excluding bowlers with no record")]
    public void CalculateSeasonStatsSummary_ShouldReturnMatchPlayRecordLeaderboard_OrderedByWinPercentageDescending()
    {
        var topId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: topId, matchPlayWins: 8, matchPlayLosses: 2),
            BowlerSeasonStatsDtoFactory.Create(matchPlayWins: 5, matchPlayLosses: 5),
            BowlerSeasonStatsDtoFactory.Create(matchPlayWins: 0, matchPlayLosses: 0),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.MatchPlayRecordLeaderboard.Count.ShouldBe(2);
        result.MatchPlayRecordLeaderboard.First().BowlerId.ShouldBe(topId);
        result.MatchPlayRecordLeaderboard.First().WinPercentage.ShouldBe(80.00m);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return MatchPlayRecordLeaderboard ordered by wins descending when win percentage is equal")]
    public void CalculateSeasonStatsSummary_ShouldReturnMatchPlayRecordLeaderboard_OrderedByWinsDescending_WhenWinPercentageIsEqual()
    {
        var topId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(matchPlayWins: 6, matchPlayLosses: 4),
            BowlerSeasonStatsDtoFactory.Create(bowlerId: topId, matchPlayWins: 9, matchPlayLosses: 6),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.MatchPlayRecordLeaderboard.Count.ShouldBe(2);
        result.MatchPlayRecordLeaderboard.First().BowlerId.ShouldBe(topId);
        result.MatchPlayRecordLeaderboard.First().Wins.ShouldBe(9);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should exclude match play records below minimum games derived from minimum tournaments")]
    public void CalculateSeasonStatsSummary_ShouldExcludeMatchPlayRecordsBelowMinimumGames_DerivedFromMinimumTournaments()
    {
        var eligibleId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: eligibleId, matchPlayWins: 3, matchPlayLosses: 1),
            BowlerSeasonStatsDtoFactory.Create(matchPlayWins: 2, matchPlayLosses: 1),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 2, minimumEntries: 0);

        result.MatchPlayRecordLeaderboard.Count.ShouldBe(1);
        result.MatchPlayRecordLeaderboard.Single().BowlerId.ShouldBe(eligibleId);
    }

    // CalculateSeasonStatsSummary — Match Play Appearances Leaderboard

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return MatchPlayAppearancesLeaderboard ordered by finals descending excluding bowlers with no finals")]
    public void CalculateSeasonStatsSummary_ShouldReturnMatchPlayAppearancesLeaderboard_OrderedByFinalsDescending()
    {
        var topId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: topId, finals: 9),
            BowlerSeasonStatsDtoFactory.Create(finals: 4),
            BowlerSeasonStatsDtoFactory.Create(finals: 0),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.MatchPlayAppearancesLeaderboard.Count.ShouldBe(2);
        result.MatchPlayAppearancesLeaderboard.First().BowlerId.ShouldBe(topId);
        result.MatchPlayAppearancesLeaderboard.First().Finals.ShouldBe(9);
    }

    // CalculateSeasonStatsSummary — Points Per Entry Leaderboard

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return PointsPerEntryLeaderboard ordered by ratio descending excluding bowlers with no entries or no points")]
    public void CalculateSeasonStatsSummary_ShouldReturnPointsPerEntryLeaderboard_OrderedByRatioDescending()
    {
        var topId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: topId, eligibleEntries: 5, bowlerOfTheYearPoints: 500),
            BowlerSeasonStatsDtoFactory.Create(eligibleEntries: 10, bowlerOfTheYearPoints: 600),
            BowlerSeasonStatsDtoFactory.Create(eligibleEntries: 0, bowlerOfTheYearPoints: 400),
            BowlerSeasonStatsDtoFactory.Create(eligibleEntries: 5, bowlerOfTheYearPoints: 0),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.PointsPerEntryLeaderboard.Count.ShouldBe(2);
        result.PointsPerEntryLeaderboard.First().BowlerId.ShouldBe(topId);
        result.PointsPerEntryLeaderboard.First().PointsPerEntry.ShouldBe(100.00m);
    }

    // CalculateSeasonStatsSummary — Points Per Tournament Leaderboard

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return PointsPerTournamentLeaderboard ordered by ratio descending excluding bowlers with no tournaments or no points")]
    public void CalculateSeasonStatsSummary_ShouldReturnPointsPerTournamentLeaderboard_OrderedByRatioDescending()
    {
        var topId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: topId, eligibleTournaments: 4, bowlerOfTheYearPoints: 400),
            BowlerSeasonStatsDtoFactory.Create(eligibleTournaments: 10, bowlerOfTheYearPoints: 600),
            BowlerSeasonStatsDtoFactory.Create(eligibleTournaments: 0, bowlerOfTheYearPoints: 300),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.PointsPerTournamentLeaderboard.Count.ShouldBe(2);
        result.PointsPerTournamentLeaderboard.First().BowlerId.ShouldBe(topId);
        result.PointsPerTournamentLeaderboard.First().PointsPerTournament.ShouldBe(100.00m);
    }

    // CalculateSeasonStatsSummary — Finals Per Entry Leaderboard

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return FinalsPerEntryLeaderboard ordered by ratio descending excluding bowlers with no entries or no finals")]
    public void CalculateSeasonStatsSummary_ShouldReturnFinalsPerEntryLeaderboard_OrderedByRatioDescending()
    {
        var topId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: topId, eligibleEntries: 4, finals: 3),
            BowlerSeasonStatsDtoFactory.Create(eligibleEntries: 10, finals: 4),
            BowlerSeasonStatsDtoFactory.Create(eligibleEntries: 0, finals: 2),
            BowlerSeasonStatsDtoFactory.Create(eligibleEntries: 5, finals: 0),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.FinalsPerEntryLeaderboard.Count.ShouldBe(2);
        result.FinalsPerEntryLeaderboard.First().BowlerId.ShouldBe(topId);
        result.FinalsPerEntryLeaderboard.First().FinalsPerEntry.ShouldBe(0.75m);
    }

    // CalculateSeasonStatsSummary — Average Finishes Leaderboard

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return AverageFinishesLeaderboard ordered ascending excluding bowlers with no average finish")]
    public void CalculateSeasonStatsSummary_ShouldReturnAverageFinishesLeaderboard_OrderedAscending()
    {
        var bestId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: bestId, averageFinish: 2.5m),
            BowlerSeasonStatsDtoFactory.Create(averageFinish: 6.0m),
            BowlerSeasonStatsDtoFactory.Create(averageFinish: null),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.AverageFinishesLeaderboard.Count.ShouldBe(2);
        result.AverageFinishesLeaderboard.First().BowlerId.ShouldBe(bestId);
        result.AverageFinishesLeaderboard.First().AverageFinish.ShouldBe(2.5m);
    }

    // CalculateSeasonStatsSummary — All Bowlers

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return AllBowlers ordered by BowlerOfTheYear points descending")]
    public void CalculateSeasonStatsSummary_ShouldReturnAllBowlers_OrderedByPointsDescending()
    {
        var highId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: highId, bowlerOfTheYearPoints: 700),
            BowlerSeasonStatsDtoFactory.Create(bowlerOfTheYearPoints: 300),
            BowlerSeasonStatsDtoFactory.Create(bowlerOfTheYearPoints: 500),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.AllBowlers.Count.ShouldBe(3);
        result.AllBowlers.First().BowlerId.ShouldBe(highId);
        result.AllBowlers.First().Points.ShouldBe(700);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should compute Average as 0 in AllBowlers when bowler has no games")]
    public void CalculateSeasonStatsSummary_ShouldComputeZeroAverage_WhenNoGamesInAllBowlers()
    {
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(totalGames: 0, totalPinfall: 0),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.AllBowlers.Single().Average.ShouldBe(0m);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should compute MatchPlayAverage as 0 in AllBowlers when bowler has no match play games")]
    public void CalculateSeasonStatsSummary_ShouldComputeZeroMatchPlayAverage_WhenNoMatchPlayGamesInAllBowlers()
    {
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(matchPlayGames: 0, matchPlayPinfall: 0),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.AllBowlers.Single().MatchPlayAverage.ShouldBe(0m);
    }

    // CalculateSeasonStatsSummary — Minimum Thresholds

    [Fact(DisplayName = "CalculateSeasonStatsSummary should exclude bowlers below minimum games from HighAverageLeaderboard")]
    public void CalculateSeasonStatsSummary_ShouldExcludeBowlersBelowMinGames_FromHighAverageLeaderboard()
    {
        var eligibleId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: eligibleId, totalGames: 45, totalPinfall: 9000),
            BowlerSeasonStatsDtoFactory.Create(totalGames: 44, totalPinfall: 8800),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 45, minimumTournaments: 0, minimumEntries: 0);

        result.HighAverageLeaderboard.Count.ShouldBe(1);
        result.HighAverageLeaderboard.Single().BowlerId.ShouldBe(eligibleId);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should use EligibleTournaments for Tournaments column in HighAverageLeaderboard")]
    public void CalculateSeasonStatsSummary_ShouldUseEligibleTournaments_InHighAverageLeaderboard()
    {
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(eligibleTournaments: 8, totalTournaments: 12, totalGames: 50, totalPinfall: 10000),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.HighAverageLeaderboard.Single().Tournaments.ShouldBe(8);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should exclude bowlers below minimum entries from PointsPerEntryLeaderboard")]
    public void CalculateSeasonStatsSummary_ShouldExcludeBowlersBelowMinEntries_FromPointsPerEntryLeaderboard()
    {
        var eligibleId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: eligibleId, eligibleEntries: 8, bowlerOfTheYearPoints: 400),
            BowlerSeasonStatsDtoFactory.Create(eligibleEntries: 7, bowlerOfTheYearPoints: 500),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 8);

        result.PointsPerEntryLeaderboard.Count.ShouldBe(1);
        result.PointsPerEntryLeaderboard.Single().BowlerId.ShouldBe(eligibleId);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should exclude bowlers below minimum tournaments from PointsPerTournamentLeaderboard")]
    public void CalculateSeasonStatsSummary_ShouldExcludeBowlersBelowMinTournaments_FromPointsPerTournamentLeaderboard()
    {
        var eligibleId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: eligibleId, eligibleTournaments: 5, bowlerOfTheYearPoints: 300),
            BowlerSeasonStatsDtoFactory.Create(eligibleTournaments: 4, bowlerOfTheYearPoints: 400),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 5, minimumEntries: 0);

        result.PointsPerTournamentLeaderboard.Count.ShouldBe(1);
        result.PointsPerTournamentLeaderboard.Single().BowlerId.ShouldBe(eligibleId);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should exclude bowlers below minimum entries from FinalsPerEntryLeaderboard")]
    public void CalculateSeasonStatsSummary_ShouldExcludeBowlersBelowMinEntries_FromFinalsPerEntryLeaderboard()
    {
        var eligibleId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: eligibleId, eligibleEntries: 8, finals: 3),
            BowlerSeasonStatsDtoFactory.Create(eligibleEntries: 7, finals: 4),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 8);

        result.FinalsPerEntryLeaderboard.Count.ShouldBe(1);
        result.FinalsPerEntryLeaderboard.Single().BowlerId.ShouldBe(eligibleId);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should compute HighAverage from bowlers meeting minimum games and exclude zero-game bowlers")]
    public void CalculateSeasonStatsSummary_ShouldComputeHighAverage_FromEligibleBowlers_WhenMinimumGamesApplied()
    {
        var expectedId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: expectedId, totalGames: 10, totalPinfall: 1900),
            BowlerSeasonStatsDtoFactory.Create(totalGames: 11, totalPinfall: 1980),
            BowlerSeasonStatsDtoFactory.Create(totalGames: 0, totalPinfall: 3000),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 10, minimumTournaments: 0, minimumEntries: 0);

        result.HighAverage.ShouldBe(190m);
        result.HighAverageBowlers.Count.ShouldBe(1);
        result.HighAverageBowlers.ShouldContainKey(expectedId);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should exclude bowlers with games below minimum from HighAverage even when their average is higher")]
    public void CalculateSeasonStatsSummary_ShouldExcludeBelowMinimumGamesBowlers_FromHighAverage_EvenWithHigherAverage()
    {
        var eligibleId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: eligibleId, totalGames: 45, totalPinfall: 8100), // avg=180, eligible
            BowlerSeasonStatsDtoFactory.Create(totalGames: 5, totalPinfall: 2000),                        // avg=400, games>0 but <45
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 45m, minimumTournaments: 0, minimumEntries: 0);

        // || mutation on line 92: "5 > 0 || 5 >= 45" = true → ineligible bowler included → highAverage = 400 not 180
        result.HighAverage.ShouldBe(180m);
        result.HighAverageBowlers.Count.ShouldBe(1);
        result.HighAverageBowlers.ShouldContainKey(eligibleId);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should include only exact-threshold match play games in MatchPlayAverageLeaderboard")]
    public void CalculateSeasonStatsSummary_ShouldIncludeOnlyEligibleMatchPlayAverageRows_WhenMinimumMatchPlayGamesApplied()
    {
        var expectedId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: expectedId, matchPlayGames: 4, matchPlayPinfall: 860),
            BowlerSeasonStatsDtoFactory.Create(matchPlayGames: 3, matchPlayPinfall: 900),
            BowlerSeasonStatsDtoFactory.Create(matchPlayGames: 0, matchPlayPinfall: 0),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 2, minimumEntries: 0);

        result.MatchPlayAverageLeaderboard.Count.ShouldBe(1);
        result.MatchPlayAverageLeaderboard.Single().BowlerId.ShouldBe(expectedId);
        result.MatchPlayAverageLeaderboard.Single().MatchPlayAverage.ShouldBe(215m);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should exclude zero-entry bowlers from PointsPerEntryLeaderboard when minimum entries is zero")]
    public void CalculateSeasonStatsSummary_ShouldExcludeZeroEntries_FromPointsPerEntryLeaderboard_WhenMinimumIsZero()
    {
        var expectedId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: expectedId, eligibleEntries: 4, bowlerOfTheYearPoints: 400),
            BowlerSeasonStatsDtoFactory.Create(eligibleEntries: 0, bowlerOfTheYearPoints: 500),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.PointsPerEntryLeaderboard.Count.ShouldBe(1);
        result.PointsPerEntryLeaderboard.Single().BowlerId.ShouldBe(expectedId);
        result.PointsPerEntryLeaderboard.Single().PointsPerEntry.ShouldBe(100m);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should exclude zero-tournament and zero-point bowlers from PointsPerTournamentLeaderboard")]
    public void CalculateSeasonStatsSummary_ShouldExcludeZeroTournamentAndZeroPointRows_FromPointsPerTournamentLeaderboard()
    {
        var expectedId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: expectedId, eligibleTournaments: 4, bowlerOfTheYearPoints: 300),
            BowlerSeasonStatsDtoFactory.Create(eligibleTournaments: 0, bowlerOfTheYearPoints: 700),
            BowlerSeasonStatsDtoFactory.Create(eligibleTournaments: 10, bowlerOfTheYearPoints: 0),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.PointsPerTournamentLeaderboard.Count.ShouldBe(1);
        result.PointsPerTournamentLeaderboard.Single().BowlerId.ShouldBe(expectedId);
        result.PointsPerTournamentLeaderboard.Single().PointsPerTournament.ShouldBe(75m);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should exclude zero-entry bowlers from FinalsPerEntryLeaderboard when minimum entries is zero")]
    public void CalculateSeasonStatsSummary_ShouldExcludeZeroEntries_FromFinalsPerEntryLeaderboard_WhenMinimumIsZero()
    {
        var expectedId = BowlerId.New();
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(bowlerId: expectedId, eligibleEntries: 4, finals: 3),
            BowlerSeasonStatsDtoFactory.Create(eligibleEntries: 0, finals: 5),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.FinalsPerEntryLeaderboard.Count.ShouldBe(1);
        result.FinalsPerEntryLeaderboard.Single().BowlerId.ShouldBe(expectedId);
        result.FinalsPerEntryLeaderboard.Single().FinalsPerEntry.ShouldBe(0.75m);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should compute MatchPlayAverage in MatchPlayRecordLeaderboard for positive and zero match play games")]
    public void CalculateSeasonStatsSummary_ShouldComputeMatchPlayAverage_InMatchPlayRecordLeaderboard()
    {
        var highAvgId = BowlerId.New();
        var zeroAvgId = BowlerId.New();

        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(
                bowlerId: highAvgId,
                matchPlayWins: 8,
                matchPlayLosses: 2,
                matchPlayGames: 4,
                matchPlayPinfall: 860),
            BowlerSeasonStatsDtoFactory.Create(
                bowlerId: zeroAvgId,
                matchPlayWins: 1,
                matchPlayLosses: 1,
                matchPlayGames: 0,
                matchPlayPinfall: 0),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.MatchPlayRecordLeaderboard.Count.ShouldBe(2);
        result.MatchPlayRecordLeaderboard.First().BowlerId.ShouldBe(highAvgId);
        result.MatchPlayRecordLeaderboard.First().MatchPlayAverage.ShouldBe(215m);

        var zeroAvgRow = result.MatchPlayRecordLeaderboard.Single(r => r.BowlerId == zeroAvgId);
        zeroAvgRow.MatchPlayAverage.ShouldBe(0m);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should compute AllBowlers averages and win percentage from non-trivial values")]
    public void CalculateSeasonStatsSummary_ShouldComputeAllBowlersDerivedValues_FromNonTrivialInputs()
    {
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(
                matchPlayWins: 3,
                matchPlayLosses: 2,
                matchPlayGames: 5,
                matchPlayPinfall: 1000,
                totalGames: 4,
                totalPinfall: 876,
                bowlerOfTheYearPoints: 600),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);
        var row = result.AllBowlers.Single();

        row.Average.ShouldBe(219m);
        row.MatchPlayAverage.ShouldBe(200m);
        row.WinPercentage.ShouldBe(60m);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should compute zero highest match play win percentage when all bowlers have no record")]
    public void CalculateSeasonStatsSummary_ShouldComputeZeroHighestMatchPlayWinPercentage_WhenAllRecordsAreZero()
    {
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(matchPlayWins: 0, matchPlayLosses: 0),
            BowlerSeasonStatsDtoFactory.Create(matchPlayWins: 0, matchPlayLosses: 0),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats, minimumGames: 0, minimumTournaments: 0, minimumEntries: 0);

        result.HighestMatchPlayWinPercentage.ShouldBe(0m);
        result.HighestMatchPlayWinPercentageBowlers.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should throw when bowlerStats is empty")]
    public void CalculateSeasonStatsSummary_ShouldThrow_WhenBowlerStatsIsEmpty()
    {
        // CalculateSeasonStatsSummary calls Enumerable.Max on non-nullable int properties which throws
        // on an empty sequence. The caller (GetSeasonStatsQueryHandler) only reaches this method when
        // a season is in the "seasons with stats" list, so empty bowlerStats is treated as a programming
        // error rather than a valid runtime state.
        Should.Throw<InvalidOperationException>(() =>
            _service.CalculateSeasonStatsSummary([], minimumGames: 0, minimumTournaments: 0, minimumEntries: 0));
    }

    [Fact(DisplayName = "GetStatMinimumsForSeasonAsync should derive minimums from the tournament count")]
    public async Task GetStatMinimumsForSeasonAsync_ShouldDeriveMinimums_FromTournamentCount()
    {
        // Arrange
        var season = SeasonDtoFactory.Create();
        _tournamentQueriesMock
            .Setup(x => x.GetTournamentCountForSeasonAsync(season.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(7);

        // Act
        var result = await _service.GetStatMinimumsForSeasonAsync(season, TestContext.Current.CancellationToken);

        // Assert
        result.NumberOfGames.ShouldBe(31.5m);
        result.NumberOfTournaments.ShouldBe(3.5m);
        result.NumberOfEntries.ShouldBe(5.25m);
    }
}