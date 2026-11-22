using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

using Handler = Neba.Api.Features.Series.AdvanceOnSquadsChanged.AdvanceOnSquadsChanged;
using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series.AdvanceOnSquadsChanged;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class AdvanceOnSquadsChangedTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
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

    private async Task<(AdvancementScenario Scenario, AdvancementFactsScript Script)> SeedAsync()
    {
        var scenario = await AdvancementRunSeed.SeedAsync(_dbContext, TestContext.Current.CancellationToken);

        return (scenario, new AdvancementFactsScript(scenario));
    }

    private static Task HandleAsync(AdvancementRun run, TournamentId tournamentId)
        => new Handler(run.Run).HandleAsync(
            new TournamentSquadsChanged { TournamentId = tournamentId }, TestContext.Current.CancellationToken);

    private Task<SeriesAggregate> StoredAsync(AdvancementScenario scenario)
        => _harness.StoredAsync(scenario.SeriesId, TestContext.Current.CancellationToken);

    [Fact(DisplayName = "HandleAsync should do nothing and take no lock for a tournament in no Series")]
    public async Task HandleAsync_ShouldDoNothing_WhenTournamentIsInNoSeries()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (scenario, _) = await SeedAsync();
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2026, ct);
        var unlinked = await SeriesSeed.AddTournamentAsync(_dbContext, season, "Not in a Series", new DateOnly(2026, 2, 1), null, ct);
        var run = _harness.CreateRun(FactsReaderDoubles.Untouched(), KeyedLockDoubles.Untouched(), SeriesCache.Untouched());

        // Act
        await HandleAsync(run, unlinked.Id);

        // Assert
        run.Saves.Saves.ShouldBe(0);
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBe(0);
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Pending);
    }

    [Fact(DisplayName = "HandleAsync should resolve the squad by Eastern date and start time to the minute and save it, even while the Stage stays Pending")]
    public async Task HandleAsync_ShouldResolveAndSaveSquad_WhenSurvivorStageIsPending()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        script.QualifierStatus = TournamentStatus.Scheduled;
        script.SquadStartTime = new TimeOnly(9, 0, 45);
        var (locks, handle) = KeyedLockDoubles.Granting(scenario.SeriesId, SeriesAdvancement.JobLockWait);
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), locks, SeriesCache.Untouched());

        // Act
        await HandleAsync(run, scenario.Survivor.Id);

        // Assert
        run.Saves.Saves.ShouldBe(1);
        handle.Verify(h => h.Dispose(), Times.Once);
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBeGreaterThan(0);
        var survivor = AdvancementJobSupport.StageOf(stored, 2);
        survivor.SquadDesignation!.SquadId.ShouldBe(scenario.SquadId);
        survivor.Status.ShouldBe(StageStatus.Pending);
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Pending);
        stored.FieldEntries.ShouldBeEmpty();
    }

    [Theory(DisplayName = "HandleAsync should leave the squad unresolved and write nothing when its start time differs by a minute")]
    [InlineData(8, 59)]
    [InlineData(9, 1)]
    public async Task HandleAsync_ShouldLeaveSquadUnresolved_WhenStartTimeDiffersByAMinute(int hour, int minute)
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        script.QualifierStatus = TournamentStatus.Scheduled;
        script.SquadStartTime = new TimeOnly(hour, minute);
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        await HandleAsync(run, scenario.Survivor.Id);

        // Assert
        run.Saves.Saves.ShouldBe(0);
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBe(0);
        AdvancementJobSupport.StageOf(stored, 2).SquadDesignation!.SquadId.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should revert the designation of a vanished squad, mark the Stage Stale and leave stored output alone")]
    public async Task HandleAsync_ShouldRevertDesignationAndMarkStale_WhenSquadVanished()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (scenario, script) = await SeedAsync();
        await AdvancementJobSupport.AdvanceAsync(_harness, scenario, script, scenario.Qualifier, ct);
        var before = await StoredAsync(scenario);
        var survivorBefore = AdvancementJobSupport.StageOf(before, 2);
        survivorBefore.Status.ShouldBe(StageStatus.Computed);
        survivorBefore.SquadDesignation!.SquadId.ShouldBe(scenario.SquadId);
        script.SquadPresent = false;
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        await HandleAsync(run, scenario.Survivor.Id);

        // Assert
        run.Saves.Saves.ShouldBe(1);
        var stored = await StoredAsync(scenario);
        var survivor = AdvancementJobSupport.StageOf(stored, 2);
        survivor.SquadDesignation!.SquadId.ShouldBeNull();
        survivor.Status.ShouldBe(StageStatus.Computed);
        survivor.IsStale.ShouldBeTrue();
        AdvancementJobSupport.OutcomesOf(survivor).ShouldBe(AdvancementJobSupport.OutcomesOf(survivorBefore));
        stored.Version.ShouldBeGreaterThan(before.Version);
        stored.FieldEntries.Count.ShouldBe(before.FieldEntries.Count);
        stored.Positions.Count.ShouldBe(before.Positions.Count);
    }

    [Fact(DisplayName = "HandleAsync should throw so the job retries when the Series lock is busy")]
    public async Task HandleAsync_ShouldThrow_WhenLockIsBusy()
    {
        // Arrange
        var (scenario, _) = await SeedAsync();
        var run = _harness.CreateRun(
            FactsReaderDoubles.Untouched(),
            KeyedLockDoubles.Refusing(scenario.SeriesId, SeriesAdvancement.JobLockWait),
            SeriesCache.Untouched());

        // Act
        Task Act() => HandleAsync(run, scenario.Survivor.Id);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(Act);
        exception.Message.ShouldContain(SeriesAdvancementErrors.Busy.Code);
        (await StoredAsync(scenario)).Version.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should throw so the job retries when another writer changed the Series first")]
    public async Task HandleAsync_ShouldThrow_WhenSeriesChangedConcurrently()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        script.QualifierStatus = TournamentStatus.Scheduled;
        var otherWriter = new BeforeSaveInterceptor(async ct =>
        {
            await using var writer = fixture.CreateDbContext();
            await writer.Series.Where(series => series.Id == scenario.SeriesId)
                .ExecuteUpdateAsync(set => set.SetProperty(series => series.Version, series => series.Version + 1), ct);
        });
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched(), extraInterceptor: otherWriter);

        // Act
        Task Act() => HandleAsync(run, scenario.Survivor.Id);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(Act);
        exception.Message.ShouldContain(SeriesAdvancementErrors.ConcurrentChange.Code);
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBe(1);
        AdvancementJobSupport.StageOf(stored, 2).SquadDesignation!.SquadId.ShouldBeNull();
    }

#nullable disable
    [Fact(DisplayName = "HandleAsync should throw ArgumentNullException when the event is null")]
    public async Task HandleAsync_ShouldThrowArgumentNullException_WhenEventIsNull()
    {
        // Arrange
        var run = _harness.CreateRun(FactsReaderDoubles.Untouched(), KeyedLockDoubles.Untouched(), SeriesCache.Untouched());

        // Act
        Task Act() => new Handler(run.Run).HandleAsync(null, TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(Act);
    }
#nullable enable
}