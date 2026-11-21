using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Series.MakeDueStagesOfficial;

internal sealed class MakeDueStagesOfficialJobHandler(
    AppDbContext appDbContext,
    SeriesAdvancement advancement,
    IOptions<SeriesAdvancementSettings> settings,
    ILogger<MakeDueStagesOfficialJobHandler> logger)
    : IBackgroundJobHandler<MakeDueStagesOfficialJob>
{
    public async Task ExecuteAsync(MakeDueStagesOfficialJob job, CancellationToken cancellationToken)
    {
        var grace = settings.Value.OfficialGrace;

        // Placement Stages never become Official, so a Series with only those is not a candidate.
        var seriesIds = await appDbContext.Series.AsNoTracking()
            .Where(s => s.Stages.Any(stage => stage.Status == StageStatus.Computed && stage.Kind != StageKind.Placement))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var failed = 0;
        var madeOfficial = 0;

        foreach (var seriesId in seriesIds)
        {
            try
            {
                var result = await advancement.RunWithoutFactsAsync(
                    seriesId,
                    (series, _, now) => series.MakeDueOfficial(now, grace),
                    SeriesAdvancement.JobLockWait,
                    cancellationToken);

                if (result.IsError)
                {
                    // A Series deleted since the query is fine. Anything else (lock busy, concurrent change) is retried.
                    if (result.FirstError.Type != ErrorType.NotFound)
                    {
                        failed++;
                        logger.LogMakeOfficialFailed(seriesId, result.FirstError.Code);
                    }
                }
                else
                {
                    madeOfficial += result.Value.OfficialStageIds.Count;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // One Series must not block the others.
                failed++;
                logger.LogMakeOfficialThrew(exception, seriesId);
            }
            finally
            {
                // Each Series must be the first load in its DbContext scope.
                appDbContext.ChangeTracker.Clear();
            }
        }

        logger.LogMakeOfficialSweepFinished(seriesIds.Count, madeOfficial, failed);

        if (failed > 0)
        {
            // Hangfire retries the sweep. Making a Stage Official is idempotent.
            throw new InvalidOperationException($"Making Stages Official failed for {failed} Series.");
        }
    }
}

internal static partial class MakeDueStagesOfficialJobHandlerLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Making Stages Official failed for Series {SeriesId}: {ErrorCode}.")]
    public static partial void LogMakeOfficialFailed(
        this ILogger<MakeDueStagesOfficialJobHandler> logger, SeriesId seriesId, string errorCode);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Making Stages Official threw for Series {SeriesId}.")]
    public static partial void LogMakeOfficialThrew(
        this ILogger<MakeDueStagesOfficialJobHandler> logger, Exception exception, SeriesId seriesId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Make Official sweep checked {SeriesCount} Series, made {StageCount} Stage(s) Official, {FailedCount} failed.")]
    public static partial void LogMakeOfficialSweepFinished(
        this ILogger<MakeDueStagesOfficialJobHandler> logger, int seriesCount, int stageCount, int failedCount);
}