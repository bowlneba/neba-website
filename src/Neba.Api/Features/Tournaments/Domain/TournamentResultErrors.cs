using ErrorOr;

namespace Neba.Api.Features.Tournaments.Domain;

internal static class TournamentResultErrors
{
    public static Error InvalidPlace(int place)
        => Error.Validation(
            code: "TournamentResult.Place.Invalid",
            description: "Place must be greater than zero.",
            metadata: new Dictionary<string, object> { { "Place", place } });

    public static Error InvalidPrizeMoney(decimal prizeMoney)
        => Error.Validation(
            code: "TournamentResult.PrizeMoney.Invalid",
            description: "Prize money must not be negative.",
            metadata: new Dictionary<string, object> { { "PrizeMoney", prizeMoney } });

    public static Error InvalidPoints(int points)
        => Error.Validation(
            code: "TournamentResult.Points.Invalid",
            description: "Points must not be negative.",
            metadata: new Dictionary<string, object> { { "Points", points } });
}