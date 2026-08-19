namespace Neba.Api.Legacy.Tournaments.Complete;

internal static partial class CompleteTournamentSyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No website tournament found for legacy id {LegacyTournamentId}; skipping completion sync.")]
    public static partial void LogLegacyTournamentNotSyncedForCompletion(this ILogger<CompleteTournamentSyncJob> logger, int legacyTournamentId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Legacy tournament {LegacyTournamentId} was already complete; chaining the result sync job anyway.")]
    public static partial void LogLegacyTournamentAlreadyCompleteForResultSync(this ILogger<CompleteTournamentSyncJob> logger, int legacyTournamentId);
}