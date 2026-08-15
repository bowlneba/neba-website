using ErrorOr;

namespace Neba.Api.Features.Tournaments.Domain;

internal static class SquadErrors
{
    public static Error InvalidMaxEntries(int maxEntries)
        => Error.Validation(
            code: "Squad.MaxEntries.Invalid",
            description: "Max entries must be greater than zero when specified.",
            metadata: new Dictionary<string, object> { { "MaxEntries", maxEntries } });
}