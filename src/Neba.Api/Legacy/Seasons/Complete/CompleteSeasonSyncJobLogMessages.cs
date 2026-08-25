using Neba.Api.Features.Seasons.Domain;

namespace Neba.Api.Legacy.Seasons.Complete;

internal static partial class CompleteSeasonSyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No legacy season found for legacy id {LegacySeasonId}; skipping season completion.")]
    public static partial void LogLegacySeasonNotFound(this ILogger<CompleteSeasonSyncJob> logger, int legacySeasonId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Legacy season {LegacySeasonId} ({StartDate:yyyy-MM-dd}-{EndDate:yyyy-MM-dd}) has no matching website season; skipping completion.")]
    public static partial void LogLegacySeasonNotMatched(this ILogger<CompleteSeasonSyncJob> logger, int legacySeasonId, DateOnly startDate, DateOnly endDate);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Legacy season {LegacySeasonId} (website season {SeasonId}) was already complete; scheduling award jobs anyway.")]
    public static partial void LogLegacySeasonAlreadyComplete(this ILogger<CompleteSeasonSyncJob> logger, int legacySeasonId, SeasonId seasonId);
}
