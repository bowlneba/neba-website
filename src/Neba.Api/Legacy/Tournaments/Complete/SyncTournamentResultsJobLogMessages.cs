namespace Neba.Api.Legacy.Tournaments.Complete;

internal static partial class SyncTournamentResultsJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No website tournament found for legacy id {LegacyTournamentId}; skipping result sync. Shouldn't happen - CompleteTournamentSyncJob already confirmed the link before chaining this job.")]
    public static partial void LogLegacyTournamentNotSyncedForResultSync(this ILogger<SyncTournamentResultsJob> logger, int legacyTournamentId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No website bowler found for legacy bowler {LegacyBowlerId} (legacy tournament {LegacyTournamentId}); skipping their result and sending a manual-intervention email.")]
    public static partial void LogLegacyBowlerNotSyncedForResultSync(this ILogger<SyncTournamentResultsJob> logger, int legacyBowlerId, int legacyTournamentId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Legacy bowler {LegacyBowlerId} has more than one Stats_ResultsStats row for legacy tournament {LegacyTournamentId}; skipping - this shouldn't happen and needs manual review in the Software.")]
    public static partial void LogLegacyBowlerHasMultipleResultRows(this ILogger<SyncTournamentResultsJob> logger, int legacyBowlerId, int legacyTournamentId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Legacy bowler {LegacyBowlerId} (legacy tournament {LegacyTournamentId}) has no Place and no qualifying stats to derive one from; skipping.")]
    public static partial void LogLegacyResultCannotBePlaced(this ILogger<SyncTournamentResultsJob> logger, int legacyBowlerId, int legacyTournamentId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Skipped syncing result for legacy bowler {LegacyBowlerId} (legacy tournament {LegacyTournamentId}): {Reason}")]
    public static partial void LogLegacyResultSyncSkipped(this ILogger<SyncTournamentResultsJob> logger, int legacyBowlerId, int legacyTournamentId, string reason);
}
