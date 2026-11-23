using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Features.Series;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class SeriesAdvancementLockTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly TimeSpan CustomWait = TimeSpan.FromSeconds(3);

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private readonly AdvancementRunHarness _harness = new(fixture);

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _harness.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    private Task<AdvancementScenario> SeedAsync()
        => AdvancementRunSeed.SeedAsync(_dbContext, TestContext.Current.CancellationToken);

    private static Task<ErrorOr<AdvanceResult>> RunAsync(
        AdvancementRun run, AdvancementScenario scenario, AdvancementOperation operation, TimeSpan? wait = null)
        => run.Run.RunWithoutFactsAsync(scenario.SeriesId, operation, wait ?? AdvancementRunHarness.Wait, TestContext.Current.CancellationToken);

    private static readonly AdvancementOperation NoChange = (_, _, _) => AdvanceResult.None;

    [Fact(DisplayName = "LockKey should be the same for one Series and different for another")]
    public void LockKey_ShouldIdentifyTheSeries()
    {
        // Arrange
        var series = SeriesId.New();
        var other = SeriesId.New();

        // Act
        var key = SeriesAdvancement.LockKey(series);

        // Assert
        key.ShouldBe(SeriesAdvancement.LockKey(series));
        key.ShouldNotBe(SeriesAdvancement.LockKey(other));
        key.ShouldBe($"series-advancement:{series}");
    }

    [Fact(DisplayName = "RunAsync should return Busy and load nothing when the lock times out")]
    public async Task RunAsync_ShouldReturnBusyAndLoadNothing_WhenLockTimesOut()
    {
        // Arrange
        var scenario = await SeedAsync();
        var locks = KeyedLockDoubles.Refusing(scenario.SeriesId, CustomWait);
        var reader = FactsReaderDoubles.Untouched();
        var run = _harness.CreateRun(reader, locks, SeriesCache.Untouched());
        var operationCalls = 0;

        // Act
        var result = await RunAsync(run, scenario, (_, _, _) =>
        {
            operationCalls++;

            return AdvanceResult.None;
        }, CustomWait);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.Busy);
        operationCalls.ShouldBe(0);
        run.Db.ChangeTracker.Entries().ShouldBeEmpty();
        run.Saves.Saves.ShouldBe(0);
        locks.Verify(
            l => l.TryAcquireAsync(SeriesAdvancement.LockKey(scenario.SeriesId), CustomWait, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "RunAsync should lock on the Series' key for the wait it was given")]
    public async Task RunAsync_ShouldLockOnTheSeriesKeyForTheGivenWait()
    {
        // Arrange
        var scenario = await SeedAsync();
        var (locks, _) = KeyedLockDoubles.Granting(scenario.SeriesId, CustomWait);
        var run = _harness.CreateRun(FactsReaderDoubles.Untouched(), locks, SeriesCache.Untouched());

        // Act
        var result = await RunAsync(run, scenario, NoChange, CustomWait);

        // Assert
        result.IsError.ShouldBeFalse();
        locks.Verify(
            l => l.TryAcquireAsync(SeriesAdvancement.LockKey(scenario.SeriesId), CustomWait, TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact(DisplayName = "RunAsync should dispose the lock handle after a normal run")]
    public async Task RunAsync_ShouldDisposeHandle_AfterNormalRun()
    {
        // Arrange
        var scenario = await SeedAsync();
        var (locks, handle) = KeyedLockDoubles.Granting(scenario.SeriesId, AdvancementRunHarness.Wait);
        var run = _harness.CreateRun(FactsReaderDoubles.Untouched(), locks, SeriesCache.Untouched());

        // Act
        var result = await RunAsync(run, scenario, NoChange);

        // Assert
        result.IsError.ShouldBeFalse();
        handle.Verify(h => h.Dispose(), Times.Once);
    }

    [Fact(DisplayName = "RunAsync should dispose the lock handle after the operation returns an error")]
    public async Task RunAsync_ShouldDisposeHandle_AfterOperationError()
    {
        // Arrange
        var scenario = await SeedAsync();
        var (locks, handle) = KeyedLockDoubles.Granting(scenario.SeriesId, AdvancementRunHarness.Wait);
        var run = _harness.CreateRun(FactsReaderDoubles.Untouched(), locks, SeriesCache.Untouched());

        // Act
        var result = await RunAsync(run, scenario, (_, _, _) => SeriesAdvancementErrors.VersionConflict);

        // Assert
        result.FirstError.ShouldBe(SeriesAdvancementErrors.VersionConflict);
        handle.Verify(h => h.Dispose(), Times.Once);
    }

    [Fact(DisplayName = "RunAsync should dispose the lock handle after the operation throws")]
    public async Task RunAsync_ShouldDisposeHandle_AfterException()
    {
        // Arrange
        var scenario = await SeedAsync();
        var (locks, handle) = KeyedLockDoubles.Granting(scenario.SeriesId, AdvancementRunHarness.Wait);
        var run = _harness.CreateRun(FactsReaderDoubles.Untouched(), locks, SeriesCache.Untouched());

        // Act
        Task Act() => RunAsync(run, scenario, (_, _, _) => throw new InvalidOperationException("The operation failed."));

        // Assert
        await Should.ThrowAsync<InvalidOperationException>(Act);
        handle.Verify(h => h.Dispose(), Times.Once);
    }

    [Fact(DisplayName = "RunAsync should hold the lock through the save and release it afterward")]
    public async Task RunAsync_ShouldReleaseLockAfterSave()
    {
        // Arrange
        var scenario = await SeedAsync();
        var events = new List<string>();
        var (locks, handle) = KeyedLockDoubles.Granting(scenario.SeriesId, AdvancementRunHarness.Wait);
        handle.Setup(h => h.Dispose()).Callback(() => events.Add("released"));
        var run = _harness.CreateRun(FactsReaderDoubles.Untouched(), locks, SeriesCache.Untouched());
        run.Db.SavedChanges += (_, _) => events.Add("saved");

        // Act
        var result = await RunAsync(run, scenario, (series, _, _) =>
        {
            series.Rename("Renamed under the lock").IsError.ShouldBeFalse();

            return new AdvanceResult { Changed = true, Version = series.Version };
        });

        // Assert
        result.IsError.ShouldBeFalse();
        events.ShouldBe(["saved", "released"]);
    }

    [Fact(DisplayName = "RunAsync should load the Series after it takes the lock, so it sees what an earlier holder saved")]
    public async Task RunAsync_ShouldLoadSeriesAfterTakingTheLock()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await SeedAsync();
        var handle = new Mock<IDisposable>(MockBehavior.Strict);
        handle.Setup(h => h.Dispose());
        var reader = FactsReaderDoubles.Untouched();
        var run = _harness.CreateRun(reader, KeyedLockDoubles.Untouched(), SeriesCache.Untouched());
        var locks = new Mock<Neba.Api.BackgroundJobs.IKeyedLockProvider>(MockBehavior.Strict);
        var loadedBeforeLock = -1;
        locks.Setup(l => l.TryAcquireAsync(SeriesAdvancement.LockKey(scenario.SeriesId), AdvancementRunHarness.Wait, ct))
            .Returns(async () =>
            {
                loadedBeforeLock = run.Db.ChangeTracker.Entries().Count();

                // The earlier holder saves while this run waits.
                await using var other = fixture.CreateDbContext();
                await other.Series.Where(series => series.Id == scenario.SeriesId)
                    .ExecuteUpdateAsync(set => set.SetProperty(series => series.Name, "Saved by the earlier holder"), ct);

                return (IDisposable?)handle.Object;
            });
        var queued = new SeriesAdvancement(
            run.Db, reader.Object, locks.Object, SeriesCache.Untouched().Object,
            new Microsoft.Extensions.Time.Testing.FakeTimeProvider(AdvancementRunHarness.Now),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SeriesAdvancement>.Instance);
        string? seenName = null;

        // Act
        var result = await queued.RunWithoutFactsAsync(scenario.SeriesId, (series, _, _) =>
        {
            seenName = series.Name;

            return AdvanceResult.None;
        }, AdvancementRunHarness.Wait, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        loadedBeforeLock.ShouldBe(0);
        seenName.ShouldBe("Saved by the earlier holder");
    }
}