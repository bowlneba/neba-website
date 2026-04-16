using System.Text.Json;

using Ardalis.SmartEnum.SystemTextJson;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Application.Caching;
using Neba.Application.Stats;
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
    private readonly FakeLogger<SeasonStatsService> _logger;
    private readonly SeasonStatsService _service;

    public SeasonStatsServiceTests()
    {
        _statsQueriesMock = new Mock<IStatsQueries>(MockBehavior.Strict);
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

        _service = new SeasonStatsService(_statsQueriesMock.Object, cache, _logger);
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

    [Fact(DisplayName = "CalculateSeasonStatsSummary should sum TotalEntries across all bowlers")]
    public void CalculateSeasonStatsSummary_ShouldSumTotalEntries()
    {
        var bowlerStats = new[]
        {
            BowlerSeasonStatsDtoFactory.Create(totalEntries: 10),
            BowlerSeasonStatsDtoFactory.Create(totalEntries: 15),
            BowlerSeasonStatsDtoFactory.Create(totalEntries: 5),
        };

        var result = _service.CalculateSeasonStatsSummary(bowlerStats);

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

        var result = _service.CalculateSeasonStatsSummary(bowlerStats);

        result.TotalPrizeMoney.ShouldBe(2500m);
    }

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

        var result = _service.CalculateSeasonStatsSummary(bowlerStats);

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

        var result = _service.CalculateSeasonStatsSummary(bowlerStats);

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

        var result = _service.CalculateSeasonStatsSummary(bowlerStats);

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

        var result = _service.CalculateSeasonStatsSummary(bowlerStats);

        result.HighAverage.ShouldBe(300m);
        result.HighAverageBowlers.Count.ShouldBe(2);
        result.HighAverageBowlers.ShouldContainKeyAndValue(bowler1Id, bowler1Name);
        result.HighAverageBowlers.ShouldContainKeyAndValue(bowler2Id, bowler2Name);
    }

    [Fact(DisplayName = "CalculateSeasonStatsSummary should return the highest match play win percentage and include all tied bowlers")]
    public void CalculateSeasonStatsSummary_ShouldReturnHighestMatchPlayWinPercentageAndBowlers()
    {
        // 8 * 1m / 10 = 0.8; 4 * 1m / 5 = 0.8; 6 * 1m / 10 = 0.6; 0 wins+losses excluded
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

        var result = _service.CalculateSeasonStatsSummary(bowlerStats);

        result.HighestMatchPlayWinPercentage.ShouldBe(0.8m);
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

        var result = _service.CalculateSeasonStatsSummary(bowlerStats);

        result.MostFinals.ShouldBe(9);
        result.MostFinalsBowlers.Count.ShouldBe(2);
        result.MostFinalsBowlers.ShouldContainKeyAndValue(bowler1Id, bowler1Name);
        result.MostFinalsBowlers.ShouldContainKeyAndValue(bowler2Id, bowler2Name);
    }
}