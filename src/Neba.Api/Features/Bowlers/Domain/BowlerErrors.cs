using ErrorOr;

namespace Neba.Api.Features.Bowlers.Domain;

internal static class BowlerErrors
{
    public static Error NotFound(BowlerId bowlerId)
        => Error.NotFound(
            code: "Bowler.NotFound",
            description: "The specified bowler was not found.",
            metadata: new Dictionary<string, object>()
            {
                { "BowlerId", bowlerId.ToString() }
            });
}