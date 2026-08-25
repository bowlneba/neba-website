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
using Neba.TestFactory.Tournaments;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Legacy.Seasons.Complete;

[IntegrationTest]
[Component("Legacy")]
[Collection<AppDbContextFixture>]
public sealed class AssignHighAverageAwardJobTests(AppDbContextFixture fixture)
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

    private AssignHighAverageAwardJob CreateJob(FakeLogger<AssignHighAverageAwardJob>? logger = null) =>
        new(_dbContext, _serviceProvider.GetRequiredService<IFusionCache>(), logger ?? new FakeLogger<AssignHighAverageAwardJob>());

    private async Task<Season> CreateCompleteSeasonAsync(CancellationToken ct)
    {
        var season = SeasonFactory.Create(complete: true);
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        return season;
    }

    // 4 stat-eligible tournaments -> minimum = floor(4.5 * 4) = 18 games.
    private async Task CreateStatEligibleTournamentsAsync(SeasonId seasonId, int count, CancellationToken ct)
    {
        for (var i = 0; i < count; i++)
        {
            await _dbContext.Tournaments.AddAsync(TournamentFactory.Create(seasonId: seasonId, statsEligible: true), ct);
        }

        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();
    }

    private async Task<BowlerId> CreateBowlerSeasonStatsAsync(
        SeasonId seasonId, int totalGames, int totalPinfall, int totalTournaments, CancellationToken ct)
    {
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        await _dbContext.BowlerSeasonStats.AddAsync(
            BowlerSeasonStatsFactory.Create(
                seasonId: seasonId, bowlerId: bowler.Id, totalGames: totalGames, totalPinfall: totalPinfall, totalTournaments: totalTournaments), ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        return bowler.Id;
    }

    [Fact(DisplayName = "AssignAsync should assign the award to the eligible bowler with the highest average")]
    public async Task AssignAsync_ShouldAssignAward_ToHighestAverageBowler()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await CreateCompleteSeasonAsync(ct);
        await CreateStatEligibleTournamentsAsync(season.Id, count: 4, ct);
        // 20 games, 4200 pinfall -> 210.0 average.
        var winnerId = await CreateBowlerSeasonStatsAsync(season.Id, totalGames: 20, totalPinfall: 4200, totalTournaments: 4, ct);
        // 20 games, 4000 pinfall -> 200.0 average.
        await CreateBowlerSeasonStatsAsync(season.Id, totalGames: 20, totalPinfall: 4000, totalTournaments: 4, ct);

        var job = CreateJob();

        // Act
        await job.AssignAsync(season.Id, ct);

        // Assert
        var reloaded = await _dbContext.Seasons.Include(s => s.HighAverageAwards).SingleAsync(s => s.Id == season.Id, ct);
        var award = reloaded.HighAverageAwards.ShouldHaveSingleItem();
        award.BowlerId.ShouldBe(winnerId);
        award.Average.ShouldBe(210.0m);
    }

    [Fact(DisplayName = "AssignAsync should exclude a bowler below the season-wide minimum-games bar")]
    public async Task AssignAsync_ShouldExcludeCandidate_WhenBelowMinimumGames()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await CreateCompleteSeasonAsync(ct);
        await CreateStatEligibleTournamentsAsync(season.Id, count: 4, ct);
        // 17 games is one short of the 18-game minimum, even with a very high average.
        await CreateBowlerSeasonStatsAsync(season.Id, totalGames: 17, totalPinfall: 5000, totalTournaments: 4, ct);

        var fakeLogger = new FakeLogger<AssignHighAverageAwardJob>();
        var job = CreateJob(fakeLogger);

        // Act
        await job.AssignAsync(season.Id, ct);

        // Assert
        var reloaded = await _dbContext.Seasons.Include(s => s.HighAverageAwards).SingleAsync(s => s.Id == season.Id, ct);
        reloaded.HighAverageAwards.ShouldBeEmpty();
        fakeLogger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Information);
    }

    [Fact(DisplayName = "AssignAsync should skip and log informationally when the season already has a High Average award")]
    public async Task AssignAsync_ShouldSkip_WhenAlreadyAssigned()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await CreateCompleteSeasonAsync(ct);
        var alreadyAwardedBowlerId = await CreateBowlerSeasonStatsAsync(season.Id, totalGames: 20, totalPinfall: 4200, totalTournaments: 4, ct);
        var tracked = await _dbContext.Seasons.SingleAsync(s => s.Id == season.Id, ct);
        tracked.AddHighAverageWinner(alreadyAwardedBowlerId, average: 210m, totalGames: 20, tournamentsParticipated: 4, statEligibleTournamentCount: 4);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        var fakeLogger = new FakeLogger<AssignHighAverageAwardJob>();
        var job = CreateJob(fakeLogger);

        // Act
        await job.AssignAsync(season.Id, ct);

        // Assert
        var reloaded = await _dbContext.Seasons.Include(s => s.HighAverageAwards).SingleAsync(s => s.Id == season.Id, ct);
        reloaded.HighAverageAwards.ShouldHaveSingleItem();
        fakeLogger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Information);
    }

    [Fact(DisplayName = "AssignAsync should evict the high-average cache tag only when a winner is written")]
    public async Task AssignAsync_ShouldEvictCacheTag_OnlyWhenWinnerWritten()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await CreateCompleteSeasonAsync(ct);
        await CreateStatEligibleTournamentsAsync(season.Id, count: 4, ct);
        await CreateBowlerSeasonStatsAsync(season.Id, totalGames: 20, totalPinfall: 4200, totalTournaments: 4, ct);

        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string cacheKey = "high-average-cache-test";
        await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("stale-cached-value"), tags: ["neba:awards:high-average"], token: ct);

        var job = CreateJob();

        // Act
        await job.AssignAsync(season.Id, ct);

        // Assert - a stale cached value would be returned by GetOrSetAsync instead of invoking the factory.
        var valueAfterAssign = await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("fresh-value"), token: ct);
        valueAfterAssign.ShouldBe("fresh-value");
    }

    [Fact(DisplayName = "AssignAsync should not evict the high-average cache tag when there are no eligible candidates")]
    public async Task AssignAsync_ShouldNotEvictCacheTag_WhenNoEligibleCandidates()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await CreateCompleteSeasonAsync(ct);

        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string cacheKey = "high-average-no-op-cache-test";
        await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("stale-cached-value"), tags: ["neba:awards:high-average"], token: ct);

        var job = CreateJob();

        // Act
        await job.AssignAsync(season.Id, ct);

        // Assert - nothing changed on the no-op branch, so the stale entry survives.
        var valueAfterAssign = await cache.GetOrSetAsync(cacheKey, _ => Task.FromResult("fresh-value"), token: ct);
        valueAfterAssign.ShouldBe("stale-cached-value");
    }
}