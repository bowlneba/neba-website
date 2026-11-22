using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Infrastructure;

using ZiggyCreatures.Caching.Fusion;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series;

/// <summary>
/// What the advancement write handler tests share: a saved Series with three Stages (Qualifying, Survivor, Placement),
/// the facts script a run reads, and runs over fresh DbContexts. The tournaments in the database stay Scheduled; every
/// run reads the scripted facts through a stub reader.
/// </summary>
internal sealed class AdvancementWriteContext(AppDbContextFixture fixture) : IAsyncDisposable
{
    private static readonly TimeSpan Grace = TimeSpan.FromHours(24);

    private readonly AppDbContext _seedDb = fixture.CreateDbContext();

    public AdvancementRunHarness Harness { get; } = new(fixture);

    public AdvancementScenario Scenario { get; private set; } = null!;

    public AdvancementFactsScript Script { get; private set; } = null!;

    public SeriesId SeriesId => Scenario.SeriesId;

    public async Task SeedAsync(CancellationToken ct)
    {
        Scenario = await AdvancementRunSeed.SeedAsync(_seedDb, ct);
        Script = new AdvancementFactsScript(Scenario);
    }

    public Mock<ISeriesAdvancementFactsReader> Reader()
        => FactsReaderDoubles.Reading(Script.Build);

    /// <summary>A run over a fresh DbContext. The defaults grant the admin lock and accept the tournament cache eviction.</summary>
    public AdvancementRun Run(
        Mock<ISeriesAdvancementFactsReader>? reader = null,
        Mock<IKeyedLockProvider>? locks = null,
        Mock<IFusionCache>? cache = null,
        Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor? interceptor = null)
        => Harness.CreateRun(
            reader ?? Reader(),
            locks ?? KeyedLockDoubles.Granting(SeriesId, SeriesAdvancement.AdminLockWait).Provider,
            cache ?? SeriesCache.Evicting(),
            extraInterceptor: interceptor);

    /// <summary>Computes every Stage, as the results-recorded job does for the Qualifier.</summary>
    public async Task AdvanceAllAsync(CancellationToken ct)
    {
        var run = Harness.CreateRun(Reader());

        var result = await run.Run.RunAsync(
            SeriesId, (series, facts, now) => series.Advance(Scenario.Qualifier.Id, facts, now), AdvancementRunHarness.Wait, ct);

        result.IsError.ShouldBeFalse();
    }

    /// <summary>Makes the Qualifying and Survivor Stages Official, as the make-official job does once the grace period passes.</summary>
    public async Task MakeOfficialAsync(CancellationToken ct)
    {
        var run = Harness.CreateRun(FactsReaderDoubles.Untouched());

        var result = await run.Run.RunWithoutFactsAsync(
            SeriesId, (series, _, now) => series.MakeDueOfficial(now.AddDays(3), Grace), AdvancementRunHarness.Wait, ct);

        result.IsError.ShouldBeFalse();
        result.Value.OfficialStageIds.ShouldBe([Scenario.Stage(1).Id, Scenario.Stage(2).Id]);
    }

    /// <summary>Adds an override through a run, so the Stage holds one the way a stored Series does.</summary>
    public async Task AddOverrideAsync(Neba.Api.Features.Bowlers.Domain.BowlerId bowlerId, CancellationToken ct)
    {
        var version = (await StoredAsync(ct)).Version;
        var run = Harness.CreateRun(Reader());

        var result = await run.Run.RunAsync(
            SeriesId,
            (series, facts, now) => series.AddOverride(Scenario.Stage(1).Id, bowlerId, "Seeded.", "seed", version, facts, now),
            AdvancementRunHarness.Wait,
            ct);

        result.IsError.ShouldBeFalse();
    }

    /// <summary>The Series as stored, with every Stage's overrides and output, read through a new context.</summary>
    public async Task<SeriesAggregate> StoredAsync(CancellationToken ct)
    {
        await using var db = fixture.CreateDbContext();

        return await db.Series.AsNoTracking()
            .Include(series => series.Stages).ThenInclude(stage => stage.SurvivorOutcomes)
            .Include(series => series.Stages).ThenInclude(stage => stage.Overrides)
            .Include(series => series.FieldEntries)
            .Include(series => series.Positions)
            .AsSplitQuery()
            .SingleAsync(series => series.Id == SeriesId, ct);
    }

    /// <summary>What an advancement job does to the Series row: a change that moves Version.</summary>
    public async Task BumpVersionAsync(CancellationToken ct)
    {
        await using var job = fixture.CreateDbContext();

        await job.Series.Where(series => series.Id == SeriesId)
            .ExecuteUpdateAsync(set => set.SetProperty(series => series.Version, series => series.Version + 1), ct);
    }

    public async ValueTask DisposeAsync()
    {
        await Harness.DisposeAsync();
        await _seedDb.DisposeAsync();
    }
}