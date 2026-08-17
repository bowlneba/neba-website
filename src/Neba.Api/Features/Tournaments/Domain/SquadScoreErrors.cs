using ErrorOr;

namespace Neba.Api.Features.Tournaments.Domain;

internal static class SquadScoreErrors
{
    public static Error InvalidValue(int score)
        => Error.Validation(
            code: "SquadScore.Value.Invalid",
            description: "A game score must be between 0 and 300.",
            metadata: new Dictionary<string, object> { { "Value", score } });
}