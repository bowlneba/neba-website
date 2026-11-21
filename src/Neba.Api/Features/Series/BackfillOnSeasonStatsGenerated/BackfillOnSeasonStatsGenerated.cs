using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Stats.Domain;

namespace Neba.Api.Features.Series.BackfillOnSeasonStatsGenerated;

/// <summary>
/// Tries the last Qualifying Stage of every Series in the season after its stats were regenerated. The event is a
/// signal to try: the facts reader checks readiness, and the fold runs the Backfill with the stored <c>BackfillAsOf</c>.
/// A rerun on an Official Stage is a dry compute that can only set Stale.
/// </summary>
internal sealed class BackfillOnSeasonStatsGenerated(
    AppDbContext appDbContext,
    SeriesAdvancement advancement,
    ILogger<BackfillOnSeasonStatsGenerated> logger)
    : IDomainEventJob<SeasonStatsGenerated>
{
    public async Task HandleAsync(SeasonStatsGenerated domainEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var seriesIds = await appDbContext.Series.AsNoTracking()
            .Where(s => s.SeasonId == domainEvent.SeasonId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var failed = 0;

        foreach (var seriesId in seriesIds)
        {
            try
            {
                var result = await advancement.RunAsync(
                    seriesId,
                    (series, facts, now) => series.LastQualifyingStage is { } last
                        ? series.Advance(last.TournamentId, facts, now)
                        : AdvanceResult.None,
                    SeriesAdvancement.JobLockWait,
                    ct);

                SeriesAdvancement.EnsureJobSucceeded(result);
            }
            catch (InvalidOperationException exception)
            {
                // One Series must not block the others.
                failed++;
                logger.LogBackfillFailed(exception, seriesId);
            }
            finally
            {
                // Each Series must be the first load in its DbContext scope.
                appDbContext.ChangeTracker.Clear();
            }
        }

        if (failed > 0)
        {
            // Hangfire retries the whole event. Advancement is idempotent.
            throw new InvalidOperationException($"Backfill failed for {failed} Series.");
        }
    }
}

internal static partial class BackfillOnSeasonStatsGeneratedLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Backfill failed for Series {SeriesId}.")]
    public static partial void LogBackfillFailed(
        this ILogger<BackfillOnSeasonStatsGenerated> logger, Exception exception, SeriesId seriesId);
}