using ErrorOr;

namespace Neba.Api.Features.Seasons.Domain;

internal static class SeasonErrors
{
    public static readonly Error SeasonNotComplete = Error.Conflict(
        code: "Season.NotComplete",
        description: "Season must be marked complete before awards can be assigned.");

    public static readonly Error HighBlockScoreMismatch = Error.Validation(
        code: "Season.HighBlockScoreMismatch",
        description: "All High Block awards for a season must have the same block score.");

    public static readonly Error BowlerAlreadyAwardedHighBlock = Error.Validation(
        code: "Season.BowlerAlreadyAwardedHighBlock",
        description: "A bowler cannot receive more than one High Block award in the same season.");

    public static readonly Error HighAverageMismatch = Error.Validation(
        code: "Season.HighAverageMismatch",
        description: "All High Average awards for a season must have the same average.");

    public static readonly Error BowlerAlreadyAwardedHighAverage = Error.Validation(
        code: "Season.BowlerAlreadyAwardedHighAverage",
        description: "A bowler cannot receive more than one High Average award in the same season.");

    public static Error HighAverageInsufficientGames(int minimumGames) => Error.Validation(
        code: "Season.HighAverageInsufficientGames",
        description: $"A bowler must have completed at least {minimumGames} games in Stat-Eligible Tournaments during the season to qualify for a High Average award.",
        metadata: new Dictionary<string, object> { { "MinimumGames", minimumGames } });

    public static Error SeasonNotFound(SeasonId id)
        => Error.NotFound(
        code: "Season.NotFound",
        description: "Season not found.",
        metadata: new()
        {
            {"id", id.Value}
        });
}