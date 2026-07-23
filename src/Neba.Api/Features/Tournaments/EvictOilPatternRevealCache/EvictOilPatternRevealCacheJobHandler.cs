using Neba.Api.BackgroundJobs;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.EvictOilPatternRevealCache;

internal sealed class EvictOilPatternRevealCacheJobHandler(IFusionCache cache)
    : IBackgroundJobHandler<EvictOilPatternRevealCacheJob>
{
    public async Task ExecuteAsync(EvictOilPatternRevealCacheJob job, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync($"neba:tournaments:{job.TournamentId}", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:tournaments:{job.SeasonId}", token: cancellationToken);
    }
}
