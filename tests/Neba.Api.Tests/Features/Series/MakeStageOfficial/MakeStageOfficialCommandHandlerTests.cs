using ErrorOr;

using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.MakeStageOfficial;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.MakeStageOfficial;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class MakeStageOfficialCommandHandlerTests(AppDbContextFixture fixture)
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

    private MakeStageOfficialCommand Command(int stageOrder, int version)
        => MakeStageOfficialCommandFactory.Create(
            seriesId: _context.SeriesId,
            stageId: _context.Scenario.Stage(stageOrder).Id,
            version: version);

    [Fact(DisplayName = "HandleAsync should lock the Series for the admin wait, read no facts, mark the Stage Official and return Updated")]
    public async Task HandleAsync_ShouldLockForAdminWait_AndMarkStageOfficial()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _context.AdvanceAllAsync(ct);
        var before = await _context.StoredAsync(ct);
        var (locks, handle) = KeyedLockDoubles.Granting(_context.SeriesId, SeriesAdvancement.AdminLockWait);
        var reader = FactsReaderDoubles.Untouched();
        var run = _context.Run(reader, locks, SeriesCache.Evicting());

        // Act
        var result = await new MakeStageOfficialCommandHandler(run.Run).HandleAsync(Command(1, before.Version), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        locks.Verify(l => l.TryAcquireAsync(SeriesAdvancement.LockKey(_context.SeriesId), SeriesAdvancement.AdminLockWait, It.IsAny<CancellationToken>()), Times.Once);
        handle.Verify(h => h.Dispose(), Times.Once);
        FactsReaderDoubles.VerifyReadTimes(reader, Times.Never);
        var stored = await _context.StoredAsync(ct);
        stored.Version.ShouldBe(before.Version + 1);
        var stage = stored.Stages.Single(s => s.Order == 1);
        stage.Status.ShouldBe(StageStatus.Official);
        stage.OfficialAt.ShouldNotBeNull();
        stored.Stages.Single(s => s.Order == 2).Status.ShouldBe(StageStatus.Computed);
    }

    [Fact(DisplayName = "HandleAsync should write nothing and keep Version when the Stage is already Official")]
    public async Task HandleAsync_ShouldWriteNothing_WhenStageIsAlreadyOfficial()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _context.AdvanceAllAsync(ct);
        var version = (await _context.StoredAsync(ct)).Version;
        (await new MakeStageOfficialCommandHandler(_context.Run().Run).HandleAsync(Command(1, version), ct)).IsError.ShouldBeFalse();
        var official = await _context.StoredAsync(ct);
        var run = _context.Run(cache: SeriesCache.Untouched());

        // Act
        var result = await new MakeStageOfficialCommandHandler(run.Run).HandleAsync(Command(1, official.Version), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        run.Saves.Saves.ShouldBe(0);
        (await _context.StoredAsync(ct)).Version.ShouldBe(official.Version);
    }

    [Fact(DisplayName = "HandleAsync should return a Conflict naming the earlier Stage when it is not Official")]
    public async Task HandleAsync_ShouldReturnConflict_WhenEarlierStageIsNotOfficial()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _context.AdvanceAllAsync(ct);
        var version = (await _context.StoredAsync(ct)).Version;
        var run = _context.Run(cache: SeriesCache.Untouched());

        // Act
        var result = await new MakeStageOfficialCommandHandler(run.Run).HandleAsync(Command(2, version), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.Official.EarlierStageNotOfficial");
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
        run.Saves.Saves.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should return the Placement Validation error for a Placement Stage")]
    public async Task HandleAsync_ShouldReturnValidation_WhenStageIsPlacement()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _context.AdvanceAllAsync(ct);
        var version = (await _context.StoredAsync(ct)).Version;
        var run = _context.Run(cache: SeriesCache.Untouched());

        // Act
        var result = await new MakeStageOfficialCommandHandler(run.Run).HandleAsync(Command(3, version), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.PlacementCannotBeOfficial);
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        run.Saves.Saves.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should return the Stage-not-computed Conflict for a Pending Stage")]
    public async Task HandleAsync_ShouldReturnConflict_WhenStageIsPending()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var run = _context.Run(cache: SeriesCache.Untouched());

        // Act
        var result = await new MakeStageOfficialCommandHandler(run.Run).HandleAsync(Command(1, 0), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.OfficialStageNotComputed);
        run.Saves.Saves.ShouldBe(0);
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
        var result = await new MakeStageOfficialCommandHandler(run.Run).HandleAsync(Command(1, version - 1), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.VersionConflict);
        run.Saves.Saves.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should return NotFound for an unknown Stage")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenStageIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _context.AdvanceAllAsync(ct);
        var version = (await _context.StoredAsync(ct)).Version;
        var run = _context.Run(cache: SeriesCache.Untouched());
        var command = MakeStageOfficialCommandFactory.Create(_context.SeriesId, SeriesStageId.New(), version);

        // Act
        var result = await new MakeStageOfficialCommandHandler(run.Run).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }
}
