using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Infrastructure;

using ZiggyCreatures.Caching.Fusion;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series;

/// <summary>One <see cref="SeriesAdvancement"/> over its own DbContext, as a job or request scope would have it.</summary>
internal sealed record AdvancementRun(SeriesAdvancement Run, AppDbContext Db, SaveCounter Saves);

/// <summary>Builds runs over fresh DbContexts, and reads stored Series through contexts that never saw the run.</summary>
internal sealed class AdvancementRunHarness(AppDbContextFixture fixture) : IAsyncDisposable
{
    public static readonly DateTimeOffset Now = new(2025, 8, 1, 12, 0, 0, TimeSpan.Zero);
    public static readonly TimeSpan Wait = TimeSpan.FromSeconds(7);

    private readonly List<AppDbContext> _contexts = [];

    public AdvancementRun CreateRun(
        Mock<ISeriesAdvancementFactsReader> reader,
        Mock<IKeyedLockProvider>? locks = null,
        Mock<IFusionCache>? cache = null,
        DateTimeOffset? now = null)
    {
        var saves = new SaveCounter();
        var db = fixture.CreateDbContext(saves);
        _contexts.Add(db);

        var run = new SeriesAdvancement(
            db,
            reader.Object,
            (locks ?? KeyedLockDoubles.GrantingAny().Provider).Object,
            (cache ?? SeriesCache.Evicting()).Object,
            new FakeTimeProvider(now ?? Now),
            NullLogger<SeriesAdvancement>.Instance);

        return new AdvancementRun(run, db, saves);
    }

    /// <summary>The Series as stored, with every Stage's output, read through a new context.</summary>
    public async Task<SeriesAggregate> StoredAsync(SeriesId seriesId, CancellationToken cancellationToken)
    {
        await using var db = fixture.CreateDbContext();

        return await db.Series.AsNoTracking()
            .Include(series => series.Stages).ThenInclude(stage => stage.SurvivorOutcomes)
            .Include(series => series.FieldEntries)
            .Include(series => series.Positions)
            .AsSplitQuery()
            .SingleAsync(series => series.Id == seriesId, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var context in _contexts)
        {
            await context.DisposeAsync();
        }
    }
}