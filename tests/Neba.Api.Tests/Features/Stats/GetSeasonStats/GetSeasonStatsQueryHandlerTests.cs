using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Database;
using Neba.Api.Features.Stats.GetSeasonStats;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Stats;

namespace Neba.Api.Tests.Features.Stats.GetSeasonStats;

[IntegrationTest]
[Component("Stats")]
[Collection<PostgreSqlFixture>]
public sealed class GetSeasonStatsQueryHandlerTests(PostgreSqlFixture fixture)
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private ServiceProvider _serviceProvider = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        var services = new ServiceCollection();
        services.AddHybridCache();
        _serviceProvider = services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    private GetSeasonStatsQueryHandler CreateHandler()
    {
        var cache = _serviceProvider.GetRequiredService<HybridCache>();
        return new GetSeasonStatsQueryHandler(
            _dbContext,
            new SeasonStatsCalculator(),
            new BowlerOfTheYearRaceCalculator(),
            cache);
    }

    [Fact(DisplayName = "HandleAsync returns SeasonHasNoStats when no BowlerSeasonStats exist")]
    public async Task HandleAsync_ShouldReturnSeasonHasNoStats_WhenNoBowlerSeasonStatsExist()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new GetSeasonStatsQuery { SeasonYear = null },
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Stats.SeasonHasNoStats");
    }

    [Fact(DisplayName = "HandleAsync returns stats for most recent season when no year specified")]
    public async Task HandleAsync_ShouldReturnStatsForMostRecentSeason_WhenNoYearSpecified()
    {
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var season = SeasonFactory.Create(
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);

        var stats = BowlerSeasonStatsFactory.Create(seasonId: season.Id, bowlerId: bowler.Id);
        await _dbContext.BowlerSeasonStats.AddAsync(stats, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new GetSeasonStatsQuery { SeasonYear = null }, ct);

        result.IsError.ShouldBeFalse();
        result.Value.Season.Id.ShouldBe(season.Id);
        result.Value.BowlerStats.ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "HandleAsync returns stats for specified year when SeasonYear matches")]
    public async Task HandleAsync_ShouldReturnStatsForSpecifiedYear_WhenSeasonYearMatches()
    {
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var season2024 = SeasonFactory.Create(
            startDate: new DateOnly(2024, 1, 1),
            endDate: new DateOnly(2024, 12, 31));
        var season2025 = SeasonFactory.Create(
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 12, 31));
        await _dbContext.Seasons.AddRangeAsync([season2024, season2025], ct);

        var stats2024 = BowlerSeasonStatsFactory.Create(seasonId: season2024.Id, bowlerId: bowler.Id);
        var stats2025 = BowlerSeasonStatsFactory.Create(seasonId: season2025.Id, bowlerId: bowler.Id);
        await _dbContext.BowlerSeasonStats.AddRangeAsync([stats2024, stats2025], ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new GetSeasonStatsQuery { SeasonYear = 2024 }, ct);

        result.IsError.ShouldBeFalse();
        result.Value.Season.Id.ShouldBe(season2024.Id);
    }

    [Fact(DisplayName = "HandleAsync returns SeasonHasNoStats when specified year has no stats")]
    public async Task HandleAsync_ShouldReturnSeasonHasNoStats_WhenSpecifiedYearHasNoStats()
    {
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var season = SeasonFactory.Create(
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);

        var stats = BowlerSeasonStatsFactory.Create(seasonId: season.Id, bowlerId: bowler.Id);
        await _dbContext.BowlerSeasonStats.AddAsync(stats, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new GetSeasonStatsQuery { SeasonYear = 2020 }, ct);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Stats.SeasonHasNoStats");
    }
}