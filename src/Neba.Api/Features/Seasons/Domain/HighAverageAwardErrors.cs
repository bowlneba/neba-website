using ErrorOr;

namespace Neba.Api.Features.Seasons.Domain;

internal static class HighAverageAwardErrors
{
    public static readonly Error BowlerIdRequired = Error.Validation(
        code: "HighAverageAward.BowlerIdRequired",
        description: "Bowler ID is required.");

    public static readonly Error InvalidAverage = Error.Validation(
        code: "HighAverageAward.InvalidAverage",
        description: "Average must be greater than zero.");

    public static readonly Error InvalidTotalGames = Error.Validation(
        code: "HighAverageAward.InvalidTotalGames",
        description: "Total games must be greater than zero.");

    public static readonly Error InvalidTournamentsParticipated = Error.Validation(
        code: "HighAverageAward.InvalidTournamentsParticipated",
        description: "Tournaments participated must be greater than zero.");
}