using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.CreateStageOverride;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.CreateStageOverride;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class CreateStageOverrideCommandHandlerTests(AppDbContextFixture fixture)
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

    // Bowlers 0 to 7 hold the eight Qualifying slots. Bowler 8 finished 9th: a result row, no slot. Bowler 9 has no result row.
    private BowlerId Holder => _context.Scenario.Bowlers[0];

    private BowlerId Ninth => _context.Scenario.Bowlers[8];

    private BowlerId NoResult => _context.Scenario.Bowlers[9];

    private async Task AdvanceAsync(CancellationToken ct)
    {
        _context.Script.QualifierPlaces = [.. _context.Scenario.Bowlers.Take(9)];
        await _context.AdvanceAllAsync(ct);
    }

    private CreateStageOverrideCommand Command(
        OverrideKind kind, BowlerId bowlerId, int version, int stageOrder = 1, Ulid? caller = null, SeriesId? seriesId = null)
        => CreateStageOverrideCommandFactory.Create(
            seriesId: seriesId ?? _context.SeriesId,
            stageId: _context.Scenario.Stage(stageOrder).Id,
            kind: kind,
            bowlerId: bowlerId,
            callerUserId: caller,
            version: version);

    [Fact(DisplayName = "HandleAsync should lock for the admin wait, read the facts once, save an Add override and return its id and the new Version")]
    public async Task HandleAsync_ShouldSaveAddOverride_AndReturnIdAndVersion()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await AdvanceAsync(ct);
        var before = await _context.StoredAsync(ct);
        var (locks, handle) = KeyedLockDoubles.Granting(_context.SeriesId, SeriesAdvancement.AdminLockWait);
        var reader = _context.Reader();
        var caller = Ulid.NewUlid();
        var command = CreateStageOverrideCommandFactory.Create(
            seriesId: _context.SeriesId,
            stageId: _context.Scenario.Stage(1).Id,
            kind: OverrideKind.Add,
            bowlerId: Ninth,
            reason: "Entry confirmed.",
            callerUserId: caller,
            version: before.Version);
        var handler = new CreateStageOverrideCommandHandler(_context.Run(reader, locks).Run);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        locks.Verify(l => l.TryAcquireAsync(SeriesAdvancement.LockKey(_context.SeriesId), SeriesAdvancement.AdminLockWait, It.IsAny<CancellationToken>()), Times.Once);
        handle.Verify(h => h.Dispose(), Times.Once);
        FactsReaderDoubles.VerifyReadTimes(reader, Times.Once);
        var stored = await _context.StoredAsync(ct);
        var saved = stored.Stages.Single(stage => stage.Order == 1).Overrides.Single();
        result.Value.OverrideId.ShouldBe(saved.Id);
        result.Value.Version.ShouldBe(stored.Version);
        result.Value.Version.ShouldBeGreaterThan(before.Version);
        saved.Kind.ShouldBe(OverrideKind.Add);
        saved.BowlerId.ShouldBe(Ninth);
        saved.Reason.ShouldBe("Entry confirmed.");
        saved.CreatedBy.ShouldBe(caller.ToString());
        stored.FieldEntries.Single(entry => entry.BowlerId == Ninth).ViaOverride.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync should save a Clear override that vacates the bowler's slot and return its id and the new Version")]
    public async Task HandleAsync_ShouldSaveClearOverride_AndVacateTheSlot()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await AdvanceAsync(ct);
        var before = await _context.StoredAsync(ct);
        var caller = Ulid.NewUlid();
        var handler = new CreateStageOverrideCommandHandler(_context.Run().Run);

        // Act
        var result = await handler.HandleAsync(Command(OverrideKind.Clear, Holder, before.Version, caller: caller), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var stored = await _context.StoredAsync(ct);
        var saved = stored.Stages.Single(stage => stage.Order == 1).Overrides.Single();
        result.Value.OverrideId.ShouldBe(saved.Id);
        result.Value.Version.ShouldBe(stored.Version);
        saved.Kind.ShouldBe(OverrideKind.Clear);
        saved.BowlerId.ShouldBe(Holder);
        saved.CreatedBy.ShouldBe(caller.ToString());
        stored.FieldEntries.Select(entry => entry.BowlerId).ShouldNotContain(Holder);
    }

    [Fact(DisplayName = "HandleAsync should route Add to the Add rule: a bowler already in the field is a Conflict")]
    public async Task HandleAsync_ShouldRouteAddToAddOverride()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await AdvanceAsync(ct);
        var version = (await _context.StoredAsync(ct)).Version;
        var handler = new CreateStageOverrideCommandHandler(_context.Run(cache: SeriesCache.Untouched()).Run);

        // Act
        var result = await handler.HandleAsync(Command(OverrideKind.Add, Holder, version), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.BowlerAlreadyInField);
    }

    [Fact(DisplayName = "HandleAsync should route Clear to the Clear rule: a bowler who holds no slot is a Validation error")]
    public async Task HandleAsync_ShouldRouteClearToClearSlot()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await AdvanceAsync(ct);
        var version = (await _context.StoredAsync(ct)).Version;
        var handler = new CreateStageOverrideCommandHandler(_context.Run(cache: SeriesCache.Untouched()).Run);

        // Act
        var result = await handler.HandleAsync(Command(OverrideKind.Clear, Ninth, version), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.BowlerNotInSlot);
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact(DisplayName = "HandleAsync should return the no-result-row Validation error when an Add names a bowler with no result in the tournament")]
    public async Task HandleAsync_ShouldReturnNoResultRow_WhenBowlerHasNoResult()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await AdvanceAsync(ct);
        var version = (await _context.StoredAsync(ct)).Version;
        var run = _context.Run(cache: SeriesCache.Untouched());

        // Act
        var result = await new CreateStageOverrideCommandHandler(run.Run).HandleAsync(Command(OverrideKind.Add, NoResult, version), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.NoResultRow);
        run.Saves.Saves.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should return the override-exists Conflict for a second Clear of the same bowler")]
    public async Task HandleAsync_ShouldReturnOverrideExists_ForSecondClear()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await AdvanceAsync(ct);
        var first = await new CreateStageOverrideCommandHandler(_context.Run().Run)
            .HandleAsync(Command(OverrideKind.Clear, Holder, (await _context.StoredAsync(ct)).Version), ct);
        first.IsError.ShouldBeFalse();
        var run = _context.Run(cache: SeriesCache.Untouched());

        // Act
        var result = await new CreateStageOverrideCommandHandler(run.Run).HandleAsync(Command(OverrideKind.Clear, Holder, first.Value.Version), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.OverrideExists);
        run.Saves.Saves.ShouldBe(0);
    }

    [Theory(DisplayName = "HandleAsync should return the version conflict and save nothing for a stale Version on an Add and on a Clear")]
    [InlineData("Add")]
    [InlineData("Clear")]
    public async Task HandleAsync_ShouldReturnVersionConflict_WhenVersionIsStale(string kindName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await AdvanceAsync(ct);
        var version = (await _context.StoredAsync(ct)).Version;
        var kind = OverrideKind.FromName(kindName);
        var bowler = kind == OverrideKind.Add ? Ninth : Holder;
        var run = _context.Run(cache: SeriesCache.Untouched());

        // Act
        var result = await new CreateStageOverrideCommandHandler(run.Run).HandleAsync(Command(kind, bowler, version + 5), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.VersionConflict);
        run.Saves.Saves.ShouldBe(0);
        (await _context.StoredAsync(ct)).Stages.Single(stage => stage.Order == 1).Overrides.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return the Stage-not-computed Conflict for an official Stage")]
    public async Task HandleAsync_ShouldReturnConflict_WhenStageIsOfficial()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await AdvanceAsync(ct);
        await _context.MakeOfficialAsync(ct);
        var version = (await _context.StoredAsync(ct)).Version;

        // Act
        var result = await new CreateStageOverrideCommandHandler(_context.Run(cache: SeriesCache.Untouched()).Run)
            .HandleAsync(Command(OverrideKind.Add, Ninth, version), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.Override.StageNotComputed");
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact(DisplayName = "HandleAsync should return the not-Qualifying Validation error for a Survivor Stage")]
    public async Task HandleAsync_ShouldReturnNotQualifying_WhenStageIsSurvivor()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await AdvanceAsync(ct);
        var version = (await _context.StoredAsync(ct)).Version;

        // Act
        var result = await new CreateStageOverrideCommandHandler(_context.Run(cache: SeriesCache.Untouched()).Run)
            .HandleAsync(Command(OverrideKind.Add, Ninth, version, stageOrder: 2), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.NotQualifyingStage);
    }

    [Fact(DisplayName = "HandleAsync should return NotFound for an unknown Series")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenSeriesIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var unknown = SeriesId.New();
        var (locks, _) = KeyedLockDoubles.Granting(unknown, SeriesAdvancement.AdminLockWait);
        var handler = new CreateStageOverrideCommandHandler(_context.Run(locks: locks).Run);

        // Act
        var result = await handler.HandleAsync(Command(OverrideKind.Add, Ninth, 0, seriesId: unknown), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact(DisplayName = "HandleAsync should return the Stage NotFound error for a Stage that is not in the Series")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenStageIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var command = CreateStageOverrideCommandFactory.Create(seriesId: _context.SeriesId, bowlerId: Ninth, version: 0);
        var handler = new CreateStageOverrideCommandHandler(_context.Run().Run);

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
        var result = await new CreateStageOverrideCommandHandler(run.Run).HandleAsync(Command(OverrideKind.Add, Ninth, 0), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.Busy);
    }

    [Theory(DisplayName = "HandleAsync should return ConcurrentChange and keep no override when a job moves Version between the load and the save")]
    [InlineData("Add")]
    [InlineData("Clear")]
    public async Task HandleAsync_ShouldReturnConcurrentChange_WhenJobMovesVersionBeforeSave(string kindName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await AdvanceAsync(ct);
        var before = await _context.StoredAsync(ct);
        var kind = OverrideKind.FromName(kindName);
        var bowler = kind == OverrideKind.Add ? Ninth : Holder;
        var racing = new BeforeSaveInterceptor(token => _context.BumpVersionAsync(token));
        var run = _context.Run(cache: SeriesCache.Untouched(), interceptor: racing);

        // Act
        var result = await new CreateStageOverrideCommandHandler(run.Run).HandleAsync(Command(kind, bowler, before.Version), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.ConcurrentChange);
        var stored = await _context.StoredAsync(ct);
        stored.Version.ShouldBe(before.Version + 1);
        stored.Stages.Single(stage => stage.Order == 1).Overrides.ShouldBeEmpty();
    }

#nullable disable
    [Fact(DisplayName = "HandleAsync should throw ArgumentNullException when the command is null")]
    public async Task HandleAsync_ShouldThrow_WhenCommandIsNull()
    {
        // Arrange
        var handler = new CreateStageOverrideCommandHandler(_context.Run().Run);

        // Act
        var act = async () => await handler.HandleAsync(null, TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }
#nullable enable
}