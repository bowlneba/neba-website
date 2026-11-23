using Neba.Api.Database;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

using Handler = Neba.Api.Features.Series.AdvanceOnSquadScoresReplaced.AdvanceOnSquadScoresReplaced;
using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series.AdvanceOnSquadScoresReplaced;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class AdvanceOnSquadScoresReplacedTests(AppDbContextFixture fixture)
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

    private static Task HandleAsync(AdvancementRun run, TournamentId tournamentId, SquadId squadId)
        => new Handler(run.Run).HandleAsync(
            new SquadScoresReplaced { TournamentId = tournamentId, SquadId = squadId }, TestContext.Current.CancellationToken);

    private Task<SeriesAggregate> StoredAsync(AdvancementScenario scenario)
        => _harness.StoredAsync(scenario.SeriesId, TestContext.Current.CancellationToken);

    // Every Stage is Computed, and the Survivor Stage resolved to the scenario's squad.
    private async Task<(AdvancementScenario Scenario, AdvancementFactsScript Script)> SeedComputedAsync()
    {
        var (scenario, script) = await SeedAsync();
        await AdvancementJobSupport.AdvanceAsync(_harness, scenario, script, scenario.Qualifier, TestContext.Current.CancellationToken);

        var stored = await StoredAsync(scenario);
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed);
        AdvancementJobSupport.StageOf(stored, 2).SquadDesignation!.SquadId.ShouldBe(scenario.SquadId);

        return (scenario, script);
    }

    private static void SwapTopTwoSurvivors(AdvancementFactsScript script)
        => script.SurvivorRanking = [script.SurvivorRanking[1], script.SurvivorRanking[0], .. script.SurvivorRanking.Skip(2)];

    [Fact(DisplayName = "HandleAsync should write nothing and keep Version when a Survivor Stage did not resolve to the squad")]
    public async Task HandleAsync_ShouldWriteNothing_WhenNoStageResolvedToTheSquad()
    {
        // Arrange
        var (scenario, script) = await SeedComputedAsync();
        var before = await StoredAsync(scenario);
        SwapTopTwoSurvivors(script);
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        await HandleAsync(run, scenario.Survivor.Id, SquadId.New());

        // Assert
        run.Saves.Saves.ShouldBe(0);
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBe(before.Version);
        AdvancementJobSupport.OutcomesOf(AdvancementJobSupport.StageOf(stored, 2))
            .ShouldBe(AdvancementJobSupport.OutcomesOf(AdvancementJobSupport.StageOf(before, 2)));
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed && !stage.IsStale);
    }

    [Fact(DisplayName = "HandleAsync should advance a Survivor Stage that was waiting for scores when its squad's scores arrive")]
    public async Task HandleAsync_ShouldAdvanceWaitingSurvivorStage_WhenSquadScoresArrive()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (scenario, script) = await SeedAsync();
        script.SquadHasScores = false;
        await AdvancementJobSupport.AdvanceAsync(_harness, scenario, script, scenario.Qualifier, ct);
        var waiting = await StoredAsync(scenario);
        AdvancementJobSupport.StageOf(waiting, 1).Status.ShouldBe(StageStatus.Computed);
        AdvancementJobSupport.StageOf(waiting, 2).Status.ShouldBe(StageStatus.Pending);
        AdvancementJobSupport.StageOf(waiting, 2).SquadDesignation!.SquadId.ShouldBe(scenario.SquadId);
        script.SquadHasScores = true;
        var (locks, handle) = KeyedLockDoubles.Granting(scenario.SeriesId, SeriesAdvancement.JobLockWait);
        var cache = SeriesCache.Evicting();
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), locks, cache);

        // Act
        await HandleAsync(run, scenario.Survivor.Id, scenario.SquadId);

        // Assert
        run.Saves.Saves.ShouldBe(1);
        handle.Verify(h => h.Dispose(), Times.Once);
        SeriesCache.VerifyEvictedOnce(cache);
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBeGreaterThan(waiting.Version);
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed);
        AdvancementJobSupport.StageOf(stored, 2).SurvivorOutcomes.Count.ShouldBe(8);
        stored.Positions.Count.ShouldBe(8);
    }

    [Fact(DisplayName = "HandleAsync should write nothing when the event is sent again for a Computed Stage with unchanged scores")]
    public async Task HandleAsync_ShouldWriteNothing_WhenScoresAreUnchanged()
    {
        // Arrange
        var (scenario, script) = await SeedComputedAsync();
        var before = await StoredAsync(scenario);
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        await HandleAsync(run, scenario.Survivor.Id, scenario.SquadId);

        // Assert
        run.Saves.Saves.ShouldBe(0);
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBe(before.Version);
        stored.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed && !stage.IsStale);
    }

    [Fact(DisplayName = "HandleAsync should recompute a Computed Stage when its squad's scores changed")]
    public async Task HandleAsync_ShouldRecomputeComputedStage_WhenScoresChanged()
    {
        // Arrange
        var (scenario, script) = await SeedComputedAsync();
        var before = await StoredAsync(scenario);
        SwapTopTwoSurvivors(script);
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        await HandleAsync(run, scenario.Survivor.Id, scenario.SquadId);

        // Assert
        run.Saves.Saves.ShouldBe(1);
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBeGreaterThan(before.Version);
        var survivor = AdvancementJobSupport.StageOf(stored, 2);
        survivor.Status.ShouldBe(StageStatus.Computed);
        survivor.IsStale.ShouldBeFalse();
        AdvancementJobSupport.OutcomesOf(survivor)
            .ShouldNotBe(AdvancementJobSupport.OutcomesOf(AdvancementJobSupport.StageOf(before, 2)));
    }

    [Fact(DisplayName = "HandleAsync should only mark an Official Stage Stale and leave its output alone when the scores changed")]
    public async Task HandleAsync_ShouldOnlyMarkOfficialStageStale_WhenScoresChanged()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (scenario, script) = await SeedComputedAsync();
        await AdvancementJobSupport.MakeOfficialAsync(_harness, scenario, ct);
        var before = await StoredAsync(scenario);
        AdvancementJobSupport.StageOf(before, 2).Status.ShouldBe(StageStatus.Official);
        SwapTopTwoSurvivors(script);
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        await HandleAsync(run, scenario.Survivor.Id, scenario.SquadId);

        // Assert
        run.Saves.Saves.ShouldBe(1);
        var stored = await StoredAsync(scenario);
        stored.Version.ShouldBeGreaterThan(before.Version);
        var survivor = AdvancementJobSupport.StageOf(stored, 2);
        survivor.Status.ShouldBe(StageStatus.Official);
        survivor.IsStale.ShouldBeTrue();
        AdvancementJobSupport.OutcomesOf(survivor).ShouldBe(AdvancementJobSupport.OutcomesOf(AdvancementJobSupport.StageOf(before, 2)));
        AdvancementJobSupport.StageOf(stored, 1).IsStale.ShouldBeFalse();
        AdvancementJobSupport.StageOf(stored, 3).Status.ShouldBe(StageStatus.Official);
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