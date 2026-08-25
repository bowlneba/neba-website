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
public sealed class AssignHighBlockAwardJobTests(AppDbContextFixture fixture)
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

    private AssignHighBlockAwardJob CreateJob(FakeLogger<AssignHighBlockAwardJob>? logger = null) =>
        new(_dbContext, _serviceProvider.GetRequiredService<IFusionCache>(), logger ?? new FakeLogger<AssignHighBlockAwardJob>());

    private async Task<Season> CreateCompleteSeasonAsync(CancellationToken ct)
    {
        var season = SeasonFactory.Create(complete: true);
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        return season;
    }

    private async Task<BowlerId> CreateBowlerSeasonStatsAsync(SeasonId seasonId, int highBlock, CancellationToken ct)
    {
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        await _dbContext.BowlerSeasonStats.AddAsync(
            BowlerSeasonStatsFactory.Create(seasonId: seasonId, bowlerId: bowler.Id, highBlock: highBlock), ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        return bowler.Id;
    }

    [Fact(DisplayName = "AssignAsync should assign the award to the highest block score, using the fixed 5-game constant")]
    public async Task AssignAsync_ShouldAssignAward_ToHighestBlockScore()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await CreateCompleteSeasonAsync(ct);
        var winnerId = await CreateBowlerSeasonStatsAsync(season.Id, highBlock: 1250, ct);
        await CreateBowlerSeasonStatsAsync(season.Id, highBlock: 1150, ct);

        var job = CreateJob();

        // Act
        await job.AssignAsync(season.Id, ct);

        // Assert
        var reloaded = await _dbContext.Seasons.Include(s => s.HighBlockAwards).SingleAsync(s => s.Id == season.Id, ct);
        var award = reloaded.HighBlockAwards.ShouldHaveSingleItem();
        award.BowlerId.ShouldBe(winnerId);
        award.BlockScore.ShouldBe(1250);
    }

    [Fact(DisplayName = "AssignAsync should assign the award to every bowler tied for the highest block score")]
    public async Task AssignAsync_ShouldAssignAward_ToEveryTiedBowler()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await CreateCompleteSeasonAsync(ct);
        var winnerId1 = await CreateBowlerSeasonStatsAsync(season.Id, highBlock: 1200, ct);
        var winnerId2 = await CreateBowlerSeasonStatsAsync(season.Id, highBlock: 1200, ct);

        var job = CreateJob();

        // Act
        await job.AssignAsync(season.Id, ct);

        // Assert
        var reloaded = await _dbContext.Seasons.Include(s => s.HighBlockAwards).SingleAsync(s => s.Id == season.Id, ct);
        reloaded.HighBlockAwards.Count.ShouldBe(2);
        reloaded.HighBlockAwards.ShouldContain(a => a.BowlerId == winnerId1);
        reloaded.HighBlockAwards.ShouldContain(a => a.BowlerId == winnerId2);
    }

    [Fact(DisplayName = "AssignAsync should exclude bowlers with no High Block score")]
    public async Task AssignAsync_ShouldExcludeCandidate_WhenHighBlockIsZero()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await CreateCompleteSeasonAsync(ct);
        await CreateBowlerSeasonStatsAsync(season.Id, highBlock: 0, ct);

        var fakeLogger = new FakeLogger<AssignHighBlockAwardJob>();
        var job = CreateJob(fakeLogger);

        // Act
        await job.AssignAsync(season.Id, ct);

        // Assert
        var reloaded = await _dbContext.Seasons.Include(s => s.HighBlockAwards).SingleAsync(s => s.Id == season.Id, ct);
        reloaded.HighBlockAwards.ShouldBeEmpty();
        fakeLogger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Information);
    }

    [Fact(DisplayName = "AssignAsync should skip and log informationally when the season already has a High Block award")]
    public async Task AssignAsync_ShouldSkip_WhenAlreadyAssigned()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await CreateCompleteSeasonAsync(ct);
        var alreadyAwardedBowlerId = await CreateBowlerSeasonStatsAsync(season.Id, highBlock: 1200, ct);
        var tracked = await _dbContext.Seasons.SingleAsync(s => s.Id == season.Id, ct);
        tracked.AddHighBlockWinner(alreadyAwardedBowlerId, score: 1200, games: 5);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        var fakeLogger = new FakeLogger<AssignHighBlockAwardJob>();
        var job = CreateJob(fakeLogger);

        // Act
        await job.AssignAsync(season.Id, ct);

        // Assert
        var reloaded = await _dbContext.Seasons.Include(s => s.HighBlockAwards).SingleAsync(s => s.Id == season.Id, ct);
        reloaded.HighBlockAwards.ShouldHaveSingleItem();
        fakeLogger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Information);
    }

    [Fact(DisplayName = "AssignAsync should evict the high-block cache tag only when a winner is written")]
    public async Task AssignAsync_ShouldEvictCacheTag_OnlyWhenWinnerWritten()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await CreateCompleteSeasonAsync(ct);
        await CreateBowlerSeasonStatsAsync(season.Id, highBlock: 1200, ct);

        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string cacheKey = "high-block-cache-test";
        await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("stale-cached-value"), tags: ["neba:awards:high-block"], token: ct);

        var job = CreateJob();

        // Act
        await job.AssignAsync(season.Id, ct);

        // Assert - a stale cached value would be returned by GetOrSetAsync instead of invoking the factory.
        var valueAfterAssign = await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("fresh-value"), token: ct);
        valueAfterAssign.ShouldBe("fresh-value");
    }

    [Fact(DisplayName = "AssignAsync should not evict the high-block cache tag when there are no eligible candidates")]
    public async Task AssignAsync_ShouldNotEvictCacheTag_WhenNoEligibleCandidates()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await CreateCompleteSeasonAsync(ct);

        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string cacheKey = "high-block-no-op-cache-test";
        await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("stale-cached-value"), tags: ["neba:awards:high-block"], token: ct);

        var job = CreateJob();

        // Act
        await job.AssignAsync(season.Id, ct);

        // Assert - nothing changed on the no-op branch, so the stale entry survives.
        var valueAfterAssign = await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("fresh-value"), token: ct);
        valueAfterAssign.ShouldBe("stale-cached-value");
    }
}