using Hangfire;
using Hangfire.InMemory;

using Neba.Api.BackgroundJobs;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.BackgroundJobs;

[UnitTest]
[Component("Infrastructure.BackgroundJobs")]
[Collection("HangfireSequential")]
public sealed class HangfireKeyedLockProviderTests : IDisposable
{
    private const string Key = "series-advancement:test";
    private static readonly TimeSpan Wait = TimeSpan.FromMilliseconds(100);

    private readonly InMemoryStorage _jobStorage = new();
    private readonly HangfireKeyedLockProvider _provider;

    public HangfireKeyedLockProviderTests()
    {
        _provider = new HangfireKeyedLockProvider(_jobStorage);
    }

    public void Dispose() => _jobStorage.Dispose();

    [Fact(DisplayName = "TryAcquireAsync should return a handle when the key is free")]
    public async Task TryAcquireAsync_ShouldReturnHandle_WhenKeyIsFree()
    {
        // Arrange & Act
        using var handle = await _provider.TryAcquireAsync(Key, Wait, TestContext.Current.CancellationToken);

        // Assert
        handle.ShouldNotBeNull();
    }

    [Fact(DisplayName = "TryAcquireAsync should return null when another holder keeps the key past the wait")]
    public async Task TryAcquireAsync_ShouldReturnNull_WhenKeyIsHeld()
    {
        // Arrange
        using var first = await _provider.TryAcquireAsync(Key, Wait, TestContext.Current.CancellationToken);

        // Act
        var second = await _provider.TryAcquireAsync(Key, Wait, TestContext.Current.CancellationToken);

        // Assert
        first.ShouldNotBeNull();
        second.ShouldBeNull();
    }

    [Fact(DisplayName = "TryAcquireAsync should grant the key again once the handle is disposed")]
    public async Task TryAcquireAsync_ShouldGrantKey_WhenHandleWasDisposed()
    {
        // Arrange
        var first = await _provider.TryAcquireAsync(Key, Wait, TestContext.Current.CancellationToken);
        first.ShouldNotBeNull();
        first.Dispose();

        // Act
        using var second = await _provider.TryAcquireAsync(Key, Wait, TestContext.Current.CancellationToken);

        // Assert
        second.ShouldNotBeNull();
    }

    [Fact(DisplayName = "TryAcquireAsync should not block a different key")]
    public async Task TryAcquireAsync_ShouldGrantOtherKey_WhenOneKeyIsHeld()
    {
        // Arrange
        using var first = await _provider.TryAcquireAsync(Key, Wait, TestContext.Current.CancellationToken);

        // Act
        using var other = await _provider.TryAcquireAsync(Key + ":other", Wait, TestContext.Current.CancellationToken);

        // Assert
        other.ShouldNotBeNull();
    }

    [Fact(DisplayName = "TryAcquireAsync should throw when the token is already cancelled")]
    public async Task TryAcquireAsync_ShouldThrow_WhenTokenIsCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        Task<IDisposable?> Act() => _provider.TryAcquireAsync(Key, Wait, cts.Token);

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(Act);
    }
}