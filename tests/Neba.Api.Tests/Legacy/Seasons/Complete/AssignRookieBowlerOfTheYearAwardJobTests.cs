using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Legacy.Seasons.Complete;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Stats;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Legacy.Seasons.Complete;

[IntegrationTest]
[Component("Legacy")]
[Collection<AppDbContextFixture>]
public sealed class AssignRookieBowlerOfTheYearAwardJobTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private ServiceProvider _serviceProvider = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();

        var services = new ServiceCollection();
        services.AddFusionCache().WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromHours(1));
        _serviceProvider = services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private AssignRookieBowlerOfTheYearAwardJob CreateJob(FakeLogger<AssignRookieBowlerOfTheYearAwardJob>? logger = null) =>
        new(_dbContext, _serviceProvider.GetRequiredService<IFusionCache>(), logger ?? new FakeLogger<AssignRookieBowlerOfTheYearAwardJob>());

    private async Task<Season> CreateCompleteSeasonAsync(CancellationToken ct)
    {
        var season = SeasonFactory.Create(complete: true);
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        return season;
    }

    private async Task<BowlerId> CreateBowlerSeasonStatsAsync(SeasonId seasonId, bool isRookie, int bowlerOfTheYearPoints, CancellationToken ct)
    {
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        await _dbContext.BowlerSeasonStats.AddAsync(
            BowlerSeasonStatsFactory.Create(seasonId: seasonId, bowlerId: bowler.Id, isRookie: isRookie, bowlerOfTheYearPoints: bowlerOfTheYearPoints), ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        return bowler.Id;
    }

    [Fact(DisplayName = "AssignAsync should assign the award to the highest-points rookie only")]
    public async Task AssignAsync_ShouldAssignAward_ToHighestPointsRookieOnly()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await CreateCompleteSeasonAsync(ct);
        var rookieWinnerId = await CreateBowlerSeasonStatsAsync(season.Id, isRookie: true, bowlerOfTheYearPoints: 400, ct);
        await CreateBowlerSeasonStatsAsync(season.Id, isRookie: true, bowlerOfTheYearPoints: 100, ct);
        // A non-rookie with more points than the rookie winner must not be considered.
        await CreateBowlerSeasonStatsAsync(season.Id, isRookie: false, bowlerOfTheYearPoints: 999, ct);

        var job = CreateJob();

        // Act
        await job.AssignAsync(season.Id, ct);

        // Assert
        var reloaded = await _dbContext.Seasons.Include(s => s.BowlerOfTheYearAwards).SingleAsync(s => s.Id == season.Id, ct);
        var award = reloaded.BowlerOfTheYearAwards.ShouldHaveSingleItem();
        award.BowlerId.ShouldBe(rookieWinnerId);
        award.Category.ShouldBe(BowlerOfTheYearCategory.Rookie);
    }

    [Fact(DisplayName = "AssignAsync should skip and log informationally when the season already has a Rookie award")]
    public async Task AssignAsync_ShouldSkip_WhenAlreadyAssigned()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await CreateCompleteSeasonAsync(ct);
        var alreadyAwardedBowlerId = await CreateBowlerSeasonStatsAsync(season.Id, isRookie: true, bowlerOfTheYearPoints: 100, ct);
        var tracked = await _dbContext.Seasons.SingleAsync(s => s.Id == season.Id, ct);
        tracked.AddRookieBowlerOfTheYearWinner(alreadyAwardedBowlerId, isRookie: true);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        var fakeLogger = new FakeLogger<AssignRookieBowlerOfTheYearAwardJob>();
        var job = CreateJob(fakeLogger);

        // Act
        await job.AssignAsync(season.Id, ct);

        // Assert
        var reloaded = await _dbContext.Seasons.Include(s => s.BowlerOfTheYearAwards).SingleAsync(s => s.Id == season.Id, ct);
        reloaded.BowlerOfTheYearAwards.ShouldHaveSingleItem();
        fakeLogger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Information);
    }

    [Fact(DisplayName = "AssignAsync should no-op and log informationally when there are no rookie candidates")]
    public async Task AssignAsync_ShouldNoOp_WhenNoRookieCandidates()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await CreateCompleteSeasonAsync(ct);
        await CreateBowlerSeasonStatsAsync(season.Id, isRookie: false, bowlerOfTheYearPoints: 500, ct);

        var fakeLogger = new FakeLogger<AssignRookieBowlerOfTheYearAwardJob>();
        var job = CreateJob(fakeLogger);

        // Act
        await job.AssignAsync(season.Id, ct);

        // Assert
        var reloaded = await _dbContext.Seasons.Include(s => s.BowlerOfTheYearAwards).SingleAsync(s => s.Id == season.Id, ct);
        reloaded.BowlerOfTheYearAwards.ShouldBeEmpty();
        fakeLogger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Information);
    }

    [Fact(DisplayName = "AssignAsync should evict the bowler-of-the-year cache tag only when a winner is written")]
    public async Task AssignAsync_ShouldEvictCacheTag_OnlyWhenWinnerWritten()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await CreateCompleteSeasonAsync(ct);
        await CreateBowlerSeasonStatsAsync(season.Id, isRookie: true, bowlerOfTheYearPoints: 500, ct);

        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string cacheKey = "rookie-boty-cache-test";
        await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("stale-cached-value"), tags: ["neba:awards:bowler-of-the-year"], token: ct);

        var job = CreateJob();

        // Act
        await job.AssignAsync(season.Id, ct);

        // Assert - a stale cached value would be returned by GetOrSetAsync instead of invoking the factory.
        var valueAfterAssign = await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("fresh-value"), token: ct);
        valueAfterAssign.ShouldBe("fresh-value");
    }

    [Fact(DisplayName = "AssignAsync should not evict the bowler-of-the-year cache tag when there are no rookie candidates")]
    public async Task AssignAsync_ShouldNotEvictCacheTag_WhenNoRookieCandidates()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await CreateCompleteSeasonAsync(ct);
        await CreateBowlerSeasonStatsAsync(season.Id, isRookie: false, bowlerOfTheYearPoints: 500, ct);

        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string cacheKey = "rookie-boty-no-op-cache-test";
        await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("stale-cached-value"), tags: ["neba:awards:bowler-of-the-year"], token: ct);

        var job = CreateJob();

        // Act
        await job.AssignAsync(season.Id, ct);

        // Assert - nothing changed on the no-op branch, so the stale entry survives.
        var valueAfterAssign = await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("fresh-value"), token: ct);
        valueAfterAssign.ShouldBe("stale-cached-value");
    }
}
