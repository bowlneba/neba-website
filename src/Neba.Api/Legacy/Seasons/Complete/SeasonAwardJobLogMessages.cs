using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;

namespace Neba.Api.Legacy.Seasons.Complete;

internal static partial class SeasonAwardJobLogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Season {SeasonId} already has a {Category} award assigned; skipping.")]
    public static partial void LogAwardAlreadyAssigned(this ILogger logger, SeasonId seasonId, string category);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Season {SeasonId} has no BowlerSeasonStats rows at all; skipping award assignment.")]
    public static partial void LogNoBowlerSeasonStatsForSeason(this ILogger logger, SeasonId seasonId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Season {SeasonId} has no eligible candidates for {Category}.")]
    public static partial void LogNoEligibleCandidatesForCategory(this ILogger logger, SeasonId seasonId, string category);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Season {SeasonId} award candidate {BowlerId} ({Category}) is missing required Bowler data (DateOfBirth/Gender); skipping.")]
    public static partial void LogAwardCandidateMissingBowlerData(this ILogger logger, SeasonId seasonId, BowlerId bowlerId, string category);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to assign {Category} award to bowler {BowlerId} in season {SeasonId}: {Reason}")]
    public static partial void LogAwardAssignmentFailed(this ILogger logger, SeasonId seasonId, BowlerId bowlerId, string category, string reason);
}