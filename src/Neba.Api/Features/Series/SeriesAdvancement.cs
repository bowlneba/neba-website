using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Caching;
using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

using ZiggyCreatures.Caching.Fusion;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Features.Series;

internal delegate ErrorOr<AdvanceResult> AdvancementOperation(SeriesAggregate series, AdvancementFacts facts, DateTimeOffset now);

/// <summary>
/// The single entry point for advancement. Every event job and every admin write runs through <see cref="RunAsync"/>.
/// It must be the first load of the Series in its DbContext scope.
/// </summary>
internal sealed class SeriesAdvancement(
    AppDbContext appDbContext,
    ISeriesAdvancementFactsReader factsReader,
    IKeyedLockProvider keyedLocks,
    IFusionCache cache,
    TimeProvider timeProvider,
    ILogger<SeriesAdvancement> logger)
{
    internal static readonly TimeSpan AdminLockWait = TimeSpan.FromSeconds(10);
    internal static readonly TimeSpan JobLockWait = TimeSpan.FromSeconds(60);

    /// <summary>The key every writer of a Series' advancement state locks on.</summary>
    internal static string LockKey(SeriesId seriesId) => $"series-advancement:{seriesId}";

    public async Task<ErrorOr<AdvanceResult>> RunAsync(
        SeriesId seriesId, AdvancementOperation operation, TimeSpan lockWait, bool needsFacts, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        using var held = await keyedLocks.TryAcquireAsync(LockKey(seriesId), lockWait, cancellationToken);
        if (held is null)
        {
            return SeriesAdvancementErrors.Busy;
        }

        // Loaded after the lock, so a run that queued behind another sees what that run saved.
        var series = await appDbContext.Series
            .Include(s => s.Stages).ThenInclude(stage => stage.SurvivorOutcomes)
            .Include(s => s.Stages).ThenInclude(stage => stage.Overrides)
            .Include(s => s.FieldEntries)
            .Include(s => s.Positions)
            .AsSplitQuery()
            .SingleOrDefaultAsync(s => s.Id == seriesId, cancellationToken);

        if (series is null)
        {
            return SeriesErrors.NotFound(seriesId);
        }

        var facts = needsFacts ? await factsReader.ReadAsync(series, cancellationToken) : AdvancementFacts.Empty;

        var outcome = operation(series, facts, timeProvider.GetUtcNow());
        if (outcome.IsError || !outcome.Value.Changed)
        {
            return outcome;
        }

        // One SaveChanges is one transaction: output is rebuilt or not at all.
        var saved = await SeriesConcurrency.SaveOrConflictAsync(appDbContext, cancellationToken);
        if (saved.IsError)
        {
            return saved.Errors;
        }

        if (outcome.Value.StatusChanged)
        {
            await cache.RemoveByTagAsync(CacheDescriptors.Tournaments.Tag, token: cancellationToken);
        }

        logger.LogAdvancementApplied(
            seriesId, outcome.Value.ComputedStageIds.Count, outcome.Value.StaleStageIds.Count, outcome.Value.Version);

        return outcome;
    }

    /// <summary>Job entry. Finds the Series the tournament belongs to (most have none) and runs the operation.</summary>
    public async Task RunForTournamentAsync(TournamentId tournamentId, AdvancementOperation operation, CancellationToken cancellationToken)
    {
        var seriesId = await appDbContext.Series.AsNoTracking()
            .Where(s => s.Stages.Any(stage => stage.TournamentId == tournamentId))
            .Select(s => (SeriesId?)s.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (seriesId is { } id)
        {
            EnsureJobSucceeded(await RunAsync(id, operation, JobLockWait, needsFacts: true, cancellationToken));
        }
    }

    /// <summary>
    /// A deleted Series is fine. Anything else throws so Hangfire retries; a rerun is idempotent, and a
    /// <c>DbUpdateConcurrencyException</c> becomes the conflict error that lands here.
    /// </summary>
    internal static void EnsureJobSucceeded(ErrorOr<AdvanceResult> result)
    {
        if (result.IsError && result.FirstError.Type != ErrorType.NotFound)
        {
            throw new InvalidOperationException(
                $"Series advancement failed: {string.Join(", ", result.Errors.Select(e => e.Code))}");
        }
    }
}

internal static partial class SeriesAdvancementLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Series {SeriesId} advancement saved: {ComputedStages} Stage(s) computed, {StaleStages} marked stale, version {Version}.")]
    public static partial void LogAdvancementApplied(
        this ILogger<SeriesAdvancement> logger, SeriesId seriesId, int computedStages, int staleStages, int version);
}