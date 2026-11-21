using ErrorOr;

using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.RecomputeSeriesAdvancement;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.RecomputeSeriesAdvancement;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class RecomputeSeriesAdvancementCommandHandlerTests(AppDbContextFixture fixture)
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

    private RecomputeSeriesAdvancementCommand Command(int stageOrder, int version, bool confirm = false, SeriesId? seriesId = null)
        => RecomputeSeriesAdvancementCommandFactory.Create(
            seriesId: seriesId ?? _context.SeriesId,
            stageId: _context.Scenario.Stage(stageOrder).Id,
            version: version,
            confirmResetOfficial: confirm);

    [Fact(DisplayName = "HandleAsync should lock the Series for the admin wait, read the facts once and return Updated")]
    public async Task HandleAsync_ShouldLockForAdminWaitAndReadFacts_AndReturnUpdated()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _context.AdvanceAllAsync(ct);
        var version = (await _context.StoredAsync(ct)).Version;
        var (locks, handle) = KeyedLockDoubles.Granting(_context.SeriesId, SeriesAdvancement.AdminLockWait);
        var reader = _context.Reader();
        var handler = new RecomputeSeriesAdvancementCommandHandler(_context.Run(reader, locks).Run);

        // Act
        var result = await handler.HandleAsync(Command(1, version), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        locks.Verify(l => l.TryAcquireAsync(SeriesAdvancement.LockKey(_context.SeriesId), SeriesAdvancement.AdminLockWait, It.IsAny<CancellationToken>()), Times.Once);
        handle.Verify(h => h.Dispose(), Times.Once);
        FactsReaderDoubles.VerifyReadTimes(reader, Times.Once);
    }

    [Fact(DisplayName = "HandleAsync should write nothing and keep Version when the recompute changes nothing")]
    public async Task HandleAsync_ShouldWriteNothing_WhenRecomputeChangesNothing()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _context.AdvanceAllAsync(ct);
        var version = (await _context.StoredAsync(ct)).Version;
        var run = _context.Run(cache: SeriesCache.Untouched());

        // Act
        var result = await new RecomputeSeriesAdvancementCommandHandler(run.Run).HandleAsync(Command(1, version), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        run.Saves.Saves.ShouldBe(0);
        (await _context.StoredAsync(ct)).Version.ShouldBe(version);
    }

    [Fact(DisplayName = "HandleAsync should rebuild the output from the current facts and move Version")]
    public async Task HandleAsync_ShouldRebuildOutput_WhenFactsChanged()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _context.AdvanceAllAsync(ct);
        var before = await _context.StoredAsync(ct);
        var firstTwo = _context.Script.QualifierPlaces.Take(2).ToList();
        _context.Script.QualifierPlaces = [firstTwo[1], firstTwo[0], .. _context.Script.QualifierPlaces.Skip(2)];
        var run = _context.Run();

        // Act
        var result = await new RecomputeSeriesAdvancementCommandHandler(run.Run).HandleAsync(Command(1, before.Version), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var stored = await _context.StoredAsync(ct);
        stored.Version.ShouldBeGreaterThan(before.Version);
        var places = stored.FieldEntries.ToDictionary(entry => entry.BowlerId, entry => entry.Place);
        places[firstTwo[1]].ShouldBe(1);
        places[firstTwo[0]].ShouldBe(2);
    }

    [Fact(DisplayName = "HandleAsync should return ConfirmationRequired listing every official Stage in range and save nothing when the caller has not confirmed")]
    public async Task HandleAsync_ShouldReturnConfirmationRequiredAndSaveNothing_WhenOfficialStagesAreInRangeWithoutConfirmation()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _context.AdvanceAllAsync(ct);
        await _context.MakeOfficialAsync(ct);
        var before = await _context.StoredAsync(ct);
        var run = _context.Run(cache: SeriesCache.Untouched());

        // Act
        var result = await new RecomputeSeriesAdvancementCommandHandler(run.Run).HandleAsync(Command(1, before.Version), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        var error = result.FirstError;
        error.Code.ShouldBe("Series.Recompute.ConfirmationRequired");
        error.Type.ShouldBe(ErrorType.Conflict);
        ((int[])error.Metadata![SeriesAdvancementErrors.OfficialStageOrdersKey]).ShouldBe([1, 2]);
        run.Saves.Saves.ShouldBe(0);
        var stored = await _context.StoredAsync(ct);
        stored.Version.ShouldBe(before.Version);
        stored.Stages.Where(stage => stage.Order <= 2).ShouldAllBe(stage => stage.Status == StageStatus.Official);
    }

    [Fact(DisplayName = "HandleAsync should list only the official Stages at or after the Stage it starts from")]
    public async Task HandleAsync_ShouldListOnlyOfficialStagesInRange()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _context.AdvanceAllAsync(ct);
        await _context.MakeOfficialAsync(ct);
        var version = (await _context.StoredAsync(ct)).Version;

        // Act
        var result = await new RecomputeSeriesAdvancementCommandHandler(_context.Run().Run).HandleAsync(Command(2, version), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        ((int[])result.FirstError.Metadata![SeriesAdvancementErrors.OfficialStageOrdersKey]).ShouldBe([2]);
    }

    [Fact(DisplayName = "HandleAsync should reset the official Stages to Computed, save and evict the tournament cache when the caller confirms")]
    public async Task HandleAsync_ShouldResetOfficialStages_WhenCallerConfirms()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _context.AdvanceAllAsync(ct);
        await _context.MakeOfficialAsync(ct);
        var before = await _context.StoredAsync(ct);
        var cache = SeriesCache.Evicting();
        var run = _context.Run(cache: cache);

        // Act
        var result = await new RecomputeSeriesAdvancementCommandHandler(run.Run).HandleAsync(Command(1, before.Version, confirm: true), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        run.Saves.Saves.ShouldBe(1);
        SeriesCache.VerifyEvictedOnce(cache);
        var stored = await _context.StoredAsync(ct);
        stored.Version.ShouldBeGreaterThan(before.Version);
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed);
        stored.Stages.Any(stage => stage.OfficialAt is not null).ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync should return the version conflict and save nothing for a stale Version")]
    public async Task HandleAsync_ShouldReturnVersionConflict_WhenVersionIsStale()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _context.AdvanceAllAsync(ct);
        var version = (await _context.StoredAsync(ct)).Version;
        var run = _context.Run(cache: SeriesCache.Untouched());

        // Act
        var result = await new RecomputeSeriesAdvancementCommandHandler(run.Run).HandleAsync(Command(1, version + 5), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.VersionConflict);
        run.Saves.Saves.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should return NotFound for an unknown Series")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenSeriesIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var unknown = SeriesId.New();
        var (locks, _) = KeyedLockDoubles.Granting(unknown, SeriesAdvancement.AdminLockWait);
        var handler = new RecomputeSeriesAdvancementCommandHandler(_context.Run(locks: locks).Run);

        // Act
        var result = await handler.HandleAsync(Command(1, 0, seriesId: unknown), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact(DisplayName = "HandleAsync should return the Stage NotFound error for a Stage that is not in the Series")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenStageIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var command = RecomputeSeriesAdvancementCommandFactory.Create(seriesId: _context.SeriesId, version: 0);
        var handler = new RecomputeSeriesAdvancementCommandHandler(_context.Run().Run);

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
        var result = await new RecomputeSeriesAdvancementCommandHandler(run.Run).HandleAsync(Command(1, 0), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.Busy);
    }

    [Fact(DisplayName = "HandleAsync should return ConcurrentChange when a job moves Version between the load and the save")]
    public async Task HandleAsync_ShouldReturnConcurrentChange_WhenJobMovesVersionBeforeSave()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _context.AdvanceAllAsync(ct);
        var before = await _context.StoredAsync(ct);
        var firstTwo = _context.Script.QualifierPlaces.Take(2).ToList();
        _context.Script.QualifierPlaces = [firstTwo[1], firstTwo[0], .. _context.Script.QualifierPlaces.Skip(2)];
        var racing = new BeforeSaveInterceptor(token => _context.BumpVersionAsync(token));
        var run = _context.Run(cache: SeriesCache.Untouched(), interceptor: racing);

        // Act
        var result = await new RecomputeSeriesAdvancementCommandHandler(run.Run).HandleAsync(Command(1, before.Version), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.ConcurrentChange);
        var stored = await _context.StoredAsync(ct);
        stored.Version.ShouldBe(before.Version + 1);
        stored.FieldEntries.ToDictionary(entry => entry.BowlerId, entry => entry.Place)[firstTwo[0]].ShouldBe(1);
    }

#nullable disable
    [Fact(DisplayName = "HandleAsync should throw ArgumentNullException when the command is null")]
    public async Task HandleAsync_ShouldThrow_WhenCommandIsNull()
    {
        // Arrange
        var handler = new RecomputeSeriesAdvancementCommandHandler(_context.Run().Run);

        // Act
        var act = async () => await handler.HandleAsync(null, TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }
#nullable enable
}