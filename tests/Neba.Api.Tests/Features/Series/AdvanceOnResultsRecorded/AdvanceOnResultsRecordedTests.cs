using Neba.Api.Database;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

using Handler = Neba.Api.Features.Series.AdvanceOnResultsRecorded.AdvanceOnResultsRecorded;
using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series.AdvanceOnResultsRecorded;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class AdvanceOnResultsRecordedTests(AppDbContextFixture fixture)
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
            new TournamentResultsRecorded { TournamentId = tournamentId }, TestContext.Current.CancellationToken);

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

    [Fact(DisplayName = "HandleAsync should compute the Stage of the tournament and the Stages after it under the Series' job lock")]
    public async Task HandleAsync_ShouldComputeStages_WhenResultsAreRecorded()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        var (locks, handle) = KeyedLockDoubles.Granting(scenario.SeriesId, SeriesAdvancement.JobLockWait);
        var cache = SeriesCache.Evicting();
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), locks, cache);

        // Act
        await HandleAsync(run, scenario.Qualifier.Id);

        // Assert
        run.Saves.Saves.ShouldBe(1);
        handle.Verify(h => h.Dispose(), Times.Once);
        SeriesCache.VerifyEvictedOnce(cache);
        var stored = await StoredAsync(scenario);
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed);
        stored.FieldEntries.Count.ShouldBe(8);
        stored.Positions.Count.ShouldBe(8);
    }

    [Fact(DisplayName = "HandleAsync should leave a Stage Pending without error when the previous Stage is not Computed")]
    public async Task HandleAsync_ShouldLeaveStagePending_WhenPreviousStageIsNotComputed()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        script.SurvivorStatus = TournamentStatus.Scheduled;
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        Task Act() => HandleAsync(run, scenario.Placement.Id);

        // Assert
        await Should.NotThrowAsync(Act);
        run.Saves.Saves.ShouldBe(0);
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBe(0);
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Pending);
        stored.FieldEntries.ShouldBeEmpty();
        stored.Positions.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should give the same result and write nothing when the event is delivered again")]
    public async Task HandleAsync_ShouldGiveSameResult_WhenEventIsRedelivered()
    {
        // Arrange
        var (scenario, script) = await SeedAsync();
        await HandleAsync(_harness.CreateRun(FactsReaderDoubles.Reading(script.Build)), scenario.Qualifier.Id);
        var afterFirst = await StoredAsync(scenario);
        var redelivery = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        await HandleAsync(redelivery, scenario.Qualifier.Id);

        // Assert
        redelivery.Saves.Saves.ShouldBe(0);
        var stored = await StoredAsync(scenario);
        afterFirst.Version.ShouldBeGreaterThan(0);
        stored.Version.ShouldBe(afterFirst.Version);
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed && !stage.IsStale);
        AdvancementJobSupport.OutcomesOf(AdvancementJobSupport.StageOf(stored, 2))
            .ShouldBe(AdvancementJobSupport.OutcomesOf(AdvancementJobSupport.StageOf(afterFirst, 2)));
        stored.FieldEntries.Select(entry => (entry.BowlerId, entry.Place)).Order()
            .ShouldBe(afterFirst.FieldEntries.Select(entry => (entry.BowlerId, entry.Place)).Order());
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
        Task Act() => HandleAsync(run, scenario.Qualifier.Id);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(Act);
        exception.Message.ShouldContain(SeriesAdvancementErrors.Busy.Code);
        (await StoredAsync(scenario)).Version.ShouldBe(0);
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