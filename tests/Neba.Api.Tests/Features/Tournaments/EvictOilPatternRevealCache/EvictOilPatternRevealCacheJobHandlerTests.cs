using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.EvictOilPatternRevealCache;
using Neba.TestFactory.Attributes;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.Tournaments.EvictOilPatternRevealCache;

[UnitTest]
[Component("Tournaments")]
public sealed class EvictOilPatternRevealCacheJobHandlerTests : IAsyncLifetime
{
    private ServiceProvider _serviceProvider = null!;

    public ValueTask InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddFusionCache()
            .WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromHours(1));
        _serviceProvider = services.BuildServiceProvider();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
        => await _serviceProvider.DisposeAsync();

    [Fact(DisplayName = "ExecuteAsync evicts the tournament detail and season list cache tags")]
    public async Task ExecuteAsync_ShouldEvictTournamentAndSeasonListCacheTags()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var handler = new EvictOilPatternRevealCacheJobHandler(cache);
        var job = new EvictOilPatternRevealCacheJob { TournamentId = TournamentId.New(), SeasonId = SeasonId.New() };

        await cache.SetAsync($"probe:{job.TournamentId}", "value", tags: [$"neba:tournaments:{job.TournamentId}"], token: ct);
        await cache.SetAsync($"probe:{job.SeasonId}", "value", tags: [$"neba:tournaments:{job.SeasonId}"], token: ct);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        (await cache.TryGetAsync<string>($"probe:{job.TournamentId}", token: ct)).HasValue.ShouldBeFalse();
        (await cache.TryGetAsync<string>($"probe:{job.SeasonId}", token: ct)).HasValue.ShouldBeFalse();
    }
}
