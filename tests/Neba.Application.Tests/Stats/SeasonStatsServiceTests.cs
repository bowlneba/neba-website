using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Application.Caching;
using Neba.Application.Stats;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Seasons;

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
}
