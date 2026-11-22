using ErrorOr;

using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.PickStageSquad;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.PickStageSquad;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class PickStageSquadCommandHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly TimeOnly TenAm = new(10, 0);

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

    // The squad starts at 10:00 while the Stage designates 9:00, so the Stage has no squad and stays Pending until a squad is picked.
    private Task AdvanceWithUnmatchedSquadAsync(CancellationToken ct)
    {
        _context.Script.SquadStartTime = TenAm;

        return _context.AdvanceAllAsync(ct);
    }

    private PickStageSquadCommand Command(int stageOrder, int version, SquadId? squadId = null, SeriesId? seriesId = null)
        => PickStageSquadCommandFactory.Create(
            seriesId: seriesId ?? _context.SeriesId,
            stageId: _context.Scenario.Stage(stageOrder).Id,
            squadId: squadId ?? _context.Scenario.SquadId,
            version: version);

    [Fact(DisplayName = "HandleAsync should lock the Series for the admin wait, read the facts once, save the squad and return Updated")]
    public async Task HandleAsync_ShouldLockForAdminWaitAndReadFacts_AndSaveSquad()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await AdvanceWithUnmatchedSquadAsync(ct);
        var before = await _context.StoredAsync(ct);
        before.Stages.Single(stage => stage.Order == 2).Status.ShouldBe(StageStatus.Pending);
        var (locks, handle) = KeyedLockDoubles.Granting(_context.SeriesId, SeriesAdvancement.AdminLockWait);
        var reader = _context.Reader();
        var cache = SeriesCache.Evicting();
        var handler = new PickStageSquadCommandHandler(_context.Run(reader, locks, cache).Run);

        // Act
        var result = await handler.HandleAsync(Command(2, before.Version), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        locks.Verify(l => l.TryAcquireAsync(SeriesAdvancement.LockKey(_context.SeriesId), SeriesAdvancement.AdminLockWait, It.IsAny<CancellationToken>()), Times.Once);
        handle.Verify(h => h.Dispose(), Times.Once);
        FactsReaderDoubles.VerifyReadTimes(reader, Times.Once);
        SeriesCache.VerifyEvictedOnce(cache);
        var stored = await _context.StoredAsync(ct);
        stored.Version.ShouldBeGreaterThan(before.Version);
        var survivor = stored.Stages.Single(stage => stage.Order == 2);
        survivor.SquadDesignation!.SquadId.ShouldBe(_context.Scenario.SquadId);
        survivor.Status.ShouldBe(StageStatus.Computed);
        survivor.SurvivorOutcomes.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should write nothing and keep Version when the squad is already the Stage's squad")]
    public async Task HandleAsync_ShouldWriteNothing_WhenSquadIsAlreadyPicked()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await AdvanceWithUnmatchedSquadAsync(ct);
        var firstVersion = (await _context.StoredAsync(ct)).Version;
        (await new PickStageSquadCommandHandler(_context.Run().Run).HandleAsync(Command(2, firstVersion), ct)).IsError.ShouldBeFalse();
        var version = (await _context.StoredAsync(ct)).Version;
        var run = _context.Run(cache: SeriesCache.Untouched());

        // Act
        var result = await new PickStageSquadCommandHandler(run.Run).HandleAsync(Command(2, version), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        run.Saves.Saves.ShouldBe(0);
        (await _context.StoredAsync(ct)).Version.ShouldBe(version);
    }

    [Fact(DisplayName = "HandleAsync should return the not-a-Survivor-Stage Validation error for a Qualifying Stage")]
    public async Task HandleAsync_ShouldReturnNotSurvivorError_WhenStageIsNotSurvivor()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await AdvanceWithUnmatchedSquadAsync(ct);
        var version = (await _context.StoredAsync(ct)).Version;
        var run = _context.Run(cache: SeriesCache.Untouched());

        // Act
        var result = await new PickStageSquadCommandHandler(run.Run).HandleAsync(Command(1, version), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.StageNotSurvivor);
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        run.Saves.Saves.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should return the squad-not-in-tournament Validation error for a squad of another tournament")]
    public async Task HandleAsync_ShouldReturnSquadNotInTournamentError_WhenSquadIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await AdvanceWithUnmatchedSquadAsync(ct);
        var version = (await _context.StoredAsync(ct)).Version;
        var run = _context.Run(cache: SeriesCache.Untouched());

        // Act
        var result = await new PickStageSquadCommandHandler(run.Run).HandleAsync(Command(2, version, squadId: SquadId.New()), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.SquadNotInTournament);
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        run.Saves.Saves.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should return the version conflict and save nothing for a stale Version")]
    public async Task HandleAsync_ShouldReturnVersionConflict_WhenVersionIsStale()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await AdvanceWithUnmatchedSquadAsync(ct);
        var version = (await _context.StoredAsync(ct)).Version;
        var run = _context.Run(cache: SeriesCache.Untouched());

        // Act
        var result = await new PickStageSquadCommandHandler(run.Run).HandleAsync(Command(2, version + 5), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.VersionConflict);
        run.Saves.Saves.ShouldBe(0);
        (await _context.StoredAsync(ct)).Stages.Single(stage => stage.Order == 2).SquadDesignation!.SquadId.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should return NotFound for an unknown Series")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenSeriesIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var unknown = SeriesId.New();
        var (locks, _) = KeyedLockDoubles.Granting(unknown, SeriesAdvancement.AdminLockWait);
        var handler = new PickStageSquadCommandHandler(_context.Run(locks: locks).Run);

        // Act
        var result = await handler.HandleAsync(Command(2, 0, seriesId: unknown), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact(DisplayName = "HandleAsync should return the Stage NotFound error for a Stage that is not in the Series")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenStageIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var command = PickStageSquadCommandFactory.Create(seriesId: _context.SeriesId, squadId: _context.Scenario.SquadId, version: 0);
        var handler = new PickStageSquadCommandHandler(_context.Run().Run);

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
        var result = await new PickStageSquadCommandHandler(run.Run).HandleAsync(Command(2, 0), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.Busy);
    }

    [Fact(DisplayName = "HandleAsync should return ConcurrentChange when a job moves Version between the load and the save")]
    public async Task HandleAsync_ShouldReturnConcurrentChange_WhenJobMovesVersionBeforeSave()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await AdvanceWithUnmatchedSquadAsync(ct);
        var before = await _context.StoredAsync(ct);
        var racing = new BeforeSaveInterceptor(token => _context.BumpVersionAsync(token));
        var run = _context.Run(cache: SeriesCache.Untouched(), interceptor: racing);

        // Act
        var result = await new PickStageSquadCommandHandler(run.Run).HandleAsync(Command(2, before.Version), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.ConcurrentChange);
        var stored = await _context.StoredAsync(ct);
        stored.Version.ShouldBe(before.Version + 1);
        stored.Stages.Single(stage => stage.Order == 2).Status.ShouldBe(StageStatus.Pending);
    }

#nullable disable
    [Fact(DisplayName = "HandleAsync should throw ArgumentNullException when the command is null")]
    public async Task HandleAsync_ShouldThrow_WhenCommandIsNull()
    {
        // Arrange
        var handler = new PickStageSquadCommandHandler(_context.Run().Run);

        // Act
        Task Act() => handler.HandleAsync(null, TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(Act);
    }
#nullable enable
}