using Neba.Api.Caching;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.Series;

/// <summary>Strict IFusionCache mocks for the Series write handlers, which clear the tournament cache tag after a save.</summary>
internal static class SeriesCache
{
    /// <summary>Accepts the tournament-tag eviction and nothing else.</summary>
    public static Mock<IFusionCache> Evicting()
    {
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);
        cache.Setup(c => c.RemoveByTagAsync(CacheDescriptors.Tournaments.Tag, null, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        return cache;
    }

    /// <summary>Has no setup, so any cache call fails the test.</summary>
    public static Mock<IFusionCache> Untouched()
        => new(MockBehavior.Strict);

    public static void VerifyEvictedOnce(Mock<IFusionCache> cache)
        => cache.Verify(c => c.RemoveByTagAsync(CacheDescriptors.Tournaments.Tag, null, It.IsAny<CancellationToken>()), Times.Once);
}
