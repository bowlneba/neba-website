using Neba.Api.Database;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

using Handler = Neba.Api.Features.Series.AdvanceOnTournamentChanged.AdvanceOnTournamentChanged;
using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series.AdvanceOnTournamentChanged;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class AdvanceOnTournamentChangedTests(AppDbContextFixture fixture)
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
            new TournamentChanged { TournamentId = tournamentId }, TestContext.Current.CancellationToken);

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
        (await StoredAsync(scenario)).Version.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should mark a Computed Stage Stale and keep its output when its tournament is cancelled")]
    public async Task HandleAsync_ShouldMarkComputedStageStale_WhenTournamentIsCancelled()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (scenario, script) = await SeedAsync();
        await AdvancementJobSupport.AdvanceAsync(_harness, scenario, script, scenario.Qualifier, ct);
        var before = await StoredAsync(scenario);
        AdvancementJobSupport.StageOf(before, 1).Status.ShouldBe(StageStatus.Computed);
        script.QualifierStatus = TournamentStatus.Cancelled;
        var run = _harness.CreateRun(FactsReaderDoubles.Reading(script.Build), cache: SeriesCache.Untouched());

        // Act
        await HandleAsync(run, scenario.Qualifier.Id);

        // Assert
        run.Saves.Saves.ShouldBe(1);
        var stored = await StoredAsync(scenario);
        var qualifying = AdvancementJobSupport.StageOf(stored, 1);
        qualifying.Status.ShouldBe(StageStatus.Computed);
        qualifying.IsStale.ShouldBeTrue();
        stored.FieldEntries.Count.ShouldBe(before.FieldEntries.Count);
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
