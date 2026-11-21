using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.RemoveStageOverride;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.RemoveStageOverride;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class RemoveStageOverrideCommandHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AdvancementWriteContext _context = new(fixture);

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        await _context.SeedAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _context.DisposeAsync();
    }

    // Bowler 8 finished 9th: a result row, no slot, until an Add override puts the bowler in the field.
    private BowlerId Ninth => _context.Scenario.Bowlers[8];

    // Computes every Stage with nine Qualifying results, then adds an override for the ninth bowler.
    private async Task<StageOverrideId> SeedWithOverrideAsync(CancellationToken ct)
    {
        _context.Script.QualifierPlaces = [.. _context.Scenario.Bowlers.Take(9)];
        await _context.AdvanceAllAsync(ct);
        await _context.AddOverrideAsync(Ninth, ct);

        return (await _context.StoredAsync(ct)).Stages.Single(stage => stage.Order == 1).Overrides.Single().Id;
    }

    private RemoveStageOverrideCommand Command(
        StageOverrideId overrideId, int version, int stageOrder = 1, SeriesId? seriesId = null)
        => RemoveStageOverrideCommandFactory.Create(
            seriesId: seriesId ?? _context.SeriesId,
            stageId: _context.Scenario.Stage(stageOrder).Id,
            overrideId: overrideId,
            version: version);

    [Fact(DisplayName = "HandleAsync should lock for the admin wait, read the facts once, remove the override and return Deleted")]
    public async Task HandleAsync_ShouldRemoveOverride_AndReturnDeleted()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var overrideId = await SeedWithOverrideAsync(ct);
        var before = await _context.StoredAsync(ct);
        before.FieldEntries.Select(entry => entry.BowlerId).ShouldContain(Ninth);
        var (locks, handle) = KeyedLockDoubles.Granting(_context.SeriesId, SeriesAdvancement.AdminLockWait);
        var reader = _context.Reader();
        var handler = new RemoveStageOverrideCommandHandler(_context.Run(reader, locks).Run);

        // Act
        var result = await handler.HandleAsync(Command(overrideId, before.Version), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        locks.Verify(l => l.TryAcquireAsync(SeriesAdvancement.LockKey(_context.SeriesId), SeriesAdvancement.AdminLockWait, It.IsAny<CancellationToken>()), Times.Once);
        handle.Verify(h => h.Dispose(), Times.Once);
        FactsReaderDoubles.VerifyReadTimes(reader, Times.Once);
        var stored = await _context.StoredAsync(ct);
        stored.Stages.Single(stage => stage.Order == 1).Overrides.ShouldBeEmpty();
        stored.FieldEntries.Select(entry => entry.BowlerId).ShouldNotContain(Ninth);
        stored.Version.ShouldBeGreaterThan(before.Version);
    }

    [Fact(DisplayName = "HandleAsync should return Deleted and write nothing for an override that is already gone, even with a stale Version")]
    public async Task HandleAsync_ShouldReturnDeletedAndSkipVersionCheck_WhenOverrideIsMissing()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedWithOverrideAsync(ct);
        var before = await _context.StoredAsync(ct);
        var run = _context.Run(cache: SeriesCache.Untouched());

        // Act
        var result = await new RemoveStageOverrideCommandHandler(run.Run).HandleAsync(Command(StageOverrideId.New(), before.Version + 5), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        run.Saves.Saves.ShouldBe(0);
        var stored = await _context.StoredAsync(ct);
        stored.Version.ShouldBe(before.Version);
        stored.Stages.Single(stage => stage.Order == 1).Overrides.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "HandleAsync should return the version conflict and keep the override for a stale Version")]
    public async Task HandleAsync_ShouldReturnVersionConflict_WhenOverrideExistsAndVersionIsStale()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var overrideId = await SeedWithOverrideAsync(ct);
        var before = await _context.StoredAsync(ct);
        var run = _context.Run(cache: SeriesCache.Untouched());

        // Act
        var result = await new RemoveStageOverrideCommandHandler(run.Run).HandleAsync(Command(overrideId, before.Version + 5), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.VersionConflict);
        run.Saves.Saves.ShouldBe(0);
        (await _context.StoredAsync(ct)).Stages.Single(stage => stage.Order == 1).Overrides.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "HandleAsync should return the Stage-not-computed Conflict and keep the override for an official Stage")]
    public async Task HandleAsync_ShouldReturnConflict_WhenStageIsOfficial()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var overrideId = await SeedWithOverrideAsync(ct);
        await _context.MakeOfficialAsync(ct);
        var before = await _context.StoredAsync(ct);
        var run = _context.Run(cache: SeriesCache.Untouched());

        // Act
        var result = await new RemoveStageOverrideCommandHandler(run.Run).HandleAsync(Command(overrideId, before.Version), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.Override.StageNotComputed");
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
        run.Saves.Saves.ShouldBe(0);
        (await _context.StoredAsync(ct)).Stages.Single(stage => stage.Order == 1).Overrides.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "HandleAsync should return NotFound for an unknown Series")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenSeriesIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var unknown = SeriesId.New();
        var (locks, _) = KeyedLockDoubles.Granting(unknown, SeriesAdvancement.AdminLockWait);
        var handler = new RemoveStageOverrideCommandHandler(_context.Run(locks: locks).Run);

        // Act
        var result = await handler.HandleAsync(Command(StageOverrideId.New(), 0, seriesId: unknown), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact(DisplayName = "HandleAsync should return the Stage NotFound error for a Stage that is not in the Series, before the missing-override success")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenStageIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var command = RemoveStageOverrideCommandFactory.Create(seriesId: _context.SeriesId, version: 0);
        var handler = new RemoveStageOverrideCommandHandler(_context.Run().Run);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.Advancement.StageNotFound");
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact(DisplayName = "HandleAsync should return Busy and read no facts when the lock times out")]
    public async Task HandleAsync_ShouldReturnBusy_WhenLockTimesOut()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var locks = KeyedLockDoubles.Refusing(_context.SeriesId, SeriesAdvancement.AdminLockWait);
        var run = _context.Run(FactsReaderDoubles.Untouched(), locks, SeriesCache.Untouched());

        // Act
        var result = await new RemoveStageOverrideCommandHandler(run.Run).HandleAsync(Command(StageOverrideId.New(), 0), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.Busy);
    }

    [Fact(DisplayName = "HandleAsync should return ConcurrentChange and keep the override when a job moves Version between the load and the save")]
    public async Task HandleAsync_ShouldReturnConcurrentChange_WhenJobMovesVersionBeforeSave()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var overrideId = await SeedWithOverrideAsync(ct);
        var before = await _context.StoredAsync(ct);
        var racing = new BeforeSaveInterceptor(token => _context.BumpVersionAsync(token));
        var run = _context.Run(cache: SeriesCache.Untouched(), interceptor: racing);

        // Act
        var result = await new RemoveStageOverrideCommandHandler(run.Run).HandleAsync(Command(overrideId, before.Version), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.ConcurrentChange);
        var stored = await _context.StoredAsync(ct);
        stored.Version.ShouldBe(before.Version + 1);
        stored.Stages.Single(stage => stage.Order == 1).Overrides.Count.ShouldBe(1);
    }

#nullable disable
    [Fact(DisplayName = "HandleAsync should throw ArgumentNullException when the command is null")]
    public async Task HandleAsync_ShouldThrow_WhenCommandIsNull()
    {
        // Arrange
        var handler = new RemoveStageOverrideCommandHandler(_context.Run().Run);

        // Act
        var act = async () => await handler.HandleAsync(null, TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }
#nullable enable
}