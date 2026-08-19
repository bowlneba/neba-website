using Neba.Api.Features.Seasons.Domain;

namespace Neba.Api.Legacy.Tournaments.Stats;

internal static partial class GenerateSeasonStatsJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No website tournament found for legacy id {LegacyTournamentId}; skipping season stats generation.")]
    public static partial void LogLegacyTournamentNotSyncedForStatsGeneration(this ILogger<GenerateSeasonStatsJob> logger, int legacyTournamentId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No website bowler found for legacy bowler {LegacyBowlerId} while generating season stats for season {SeasonId}; skipping.")]
    public static partial void LogLegacyBowlerNotSyncedForStatsGeneration(this ILogger<GenerateSeasonStatsJob> logger, int legacyBowlerId, SeasonId seasonId);
}