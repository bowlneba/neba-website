using ErrorOr;

namespace Neba.Api.Features.Seasons.Domain;

internal static class HighBlockAwardErrors
{
    public static readonly Error BowlerIdRequired = Error.Validation(
        code: "HighBlockAward.BowlerIdRequired",
        description: "Bowler ID is required.");

    public static readonly Error InvalidBlockScore = Error.Validation(
        code: "HighBlockAward.InvalidBlockScore",
        description: "Block score must be a positive integer.");

    public static Error BlockScoreExceedsMaximum(int games) => Error.Validation(
        code: "HighBlockAward.BlockScoreExceedsMaximum",
        description: $"Block score cannot exceed the maximum possible score of {games * 300} for {games} games.",
        metadata: new Dictionary<string, object> { { "MaximumBlockScore", games * 300 } });
}