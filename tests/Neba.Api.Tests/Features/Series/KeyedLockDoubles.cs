using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Tests.Features.Series;

/// <summary>Strict <see cref="IKeyedLockProvider"/> mocks for the advancement run. A call with another key or wait fails the test.</summary>
internal static class KeyedLockDoubles
{
    /// <summary>Grants the Series' lock once and hands back the handle, so a test can check it was disposed.</summary>
    public static (Mock<IKeyedLockProvider> Provider, Mock<IDisposable> Handle) Granting(SeriesId seriesId, TimeSpan wait)
    {
        var handle = new Mock<IDisposable>(MockBehavior.Strict);
        handle.Setup(h => h.Dispose());

        var provider = new Mock<IKeyedLockProvider>(MockBehavior.Strict);
        provider.Setup(p => p.TryAcquireAsync(SeriesAdvancement.LockKey(seriesId), wait, It.IsAny<CancellationToken>()))
            .ReturnsAsync(handle.Object);

        return (provider, handle);
    }

    /// <summary>Times out on the Series' lock.</summary>
    public static Mock<IKeyedLockProvider> Refusing(SeriesId seriesId, TimeSpan wait)
    {
        var provider = new Mock<IKeyedLockProvider>(MockBehavior.Strict);
        provider.Setup(p => p.TryAcquireAsync(SeriesAdvancement.LockKey(seriesId), wait, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDisposable?)null);

        return provider;
    }

    /// <summary>Grants any key for any wait, for tests that are not about the lock.</summary>
    public static (Mock<IKeyedLockProvider> Provider, Mock<IDisposable> Handle) GrantingAny()
    {
        var handle = new Mock<IDisposable>(MockBehavior.Strict);
        handle.Setup(h => h.Dispose());

        var provider = new Mock<IKeyedLockProvider>(MockBehavior.Strict);
        provider.Setup(p => p.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handle.Object);

        return (provider, handle);
    }

    /// <summary>Times out on every key, for tests of a host whose lock is always held.</summary>
    public static Mock<IKeyedLockProvider> RefusingAny()
    {
        var provider = new Mock<IKeyedLockProvider>(MockBehavior.Strict);
        provider.Setup(p => p.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDisposable?)null);

        return provider;
    }

    /// <summary>Has no setup, so any lock request fails the test.</summary>
    public static Mock<IKeyedLockProvider> Untouched()
        => new(MockBehavior.Strict);
}