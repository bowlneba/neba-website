using System.Globalization;

using ErrorOr;

using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.Domain;

internal static class SeriesErrors
{
    public const string StageOrderKey = "StageOrder";

    /// <summary>Metadata key naming the request property a non-Stage error belongs to (for example <c>Series.Finale.Date</c>).</summary>
    public const string PropertyKey = "Property";

    public static readonly Error NameRequired = Error.Validation(
        code: "Series.Name.Required",
        description: "Series name must not be empty.");

    public static Error NameTooLong(int maxLength) => Error.Validation(
        code: "Series.Name.TooLong",
        description: $"Series name must be {maxLength} characters or fewer.");

    public static Error SeasonNotFound(string seasonId) => Error.Validation(
        code: "Series.Season.NotFound",
        description: "The selected season does not exist.",
        metadata: new Dictionary<string, object> { { "SeasonId", seasonId } });

    public static Error NotFound(SeriesId id) => Error.NotFound(
        code: "Series.NotFound",
        description: "Series not found.",
        metadata: new Dictionary<string, object> { { "SeriesId", id.ToString() } });

    public static readonly Error Locked = Error.Conflict(
        code: "Series.Locked",
        description: "This Series is locked because processing has begun. Only its name, its logo and the Finale's date, start time and center can change, and it cannot be deleted.");

    public static Error LogoNotUploaded
        => Error.Validation("Series.Logo.NotUploaded", "Logo must be a file uploaded through the Series logo upload endpoint.");

    public static Error LogoChangeDenied
        => Error.Forbidden("Series.Logo.ChangeDenied", "Your account is not allowed to change Series logos.");

    // Stage errors. "Stage 9 (Callahan's Bowl-O-Rama): ..." names the offending row.
    public static Error StageNotFound(int order, SeriesStageId stageId)
        => Stage(order, null, "Series.Stage.NotFound", "this Stage no longer exists. Reload the Series and try again.", ErrorType.Validation);

    public static Error TournamentNotFound(int order, TournamentId tournamentId)
        => Stage(order, null, "Series.Stage.TournamentNotFound", "the selected tournament does not exist.", ErrorType.Validation);

    public static Error DuplicateTournament(int order, string tournament)
        => Stage(order, tournament, "Series.Stage.DuplicateTournament", "this tournament is already a Stage in this Series.", ErrorType.Validation);

    public static Error TournamentInAnotherSeries(int order, string tournament, string otherSeries)
        => Stage(order, tournament, "Series.Stage.TournamentInAnotherSeries", $"this tournament is already a Stage in the {otherSeries} Series.", ErrorType.Conflict);

    public static Error TournamentNotInSeason(int order, string tournament)
        => Stage(order, tournament, "Series.Stage.TournamentNotInSeason", "this tournament is not in the Series' season.", ErrorType.Validation);

    public static Error KindOutOfOrder(int order, string tournament, StageKind kind, StageKind previous)
        => Stage(order, tournament, "Series.Stage.KindOutOfOrder", $"a {kind.Name} Stage cannot follow a {previous.Name} Stage. Stages run Qualifying, then Survivor, then Placement.", ErrorType.Validation);

    public static Error MultiplePlacementStages(int order, string tournament)
        => Stage(order, tournament, "Series.Stage.MultiplePlacement", "a Series has at most one Placement Stage.", ErrorType.Validation);

    public static Error PlacementNotLast(int order, string tournament)
        => Stage(order, tournament, "Series.Stage.PlacementNotLast", "the Placement Stage must be the last Stage.", ErrorType.Validation);

    public static Error DateNotAfterPrevious(int order, string tournament, DateOnly startDate, DateOnly previousStartDate)
        => Stage(order, tournament, "Series.Stage.DateNotAfterPrevious", $"this tournament starts {Format(startDate)}, which is not after the previous Stage's tournament ({Format(previousStartDate)}). Stages must follow tournament dates.", ErrorType.Validation);

    public static Error SurvivorCountNotDecreasing(int order, string tournament, int count, int previousCount)
        => Stage(order, tournament, "Series.Stage.SurvivorCountNotDecreasing", $"{count} advancing must be fewer than the previous Survivor Stage's {previousCount}.", ErrorType.Validation);

    public static Error AdvancingCountNotAllowed(int order, string tournament)
        => Stage(order, tournament, "Series.Stage.AdvancingCountNotAllowed", "a Placement Stage has no advancing count. Set the Finale on the Series instead.", ErrorType.Validation);

    // Finale errors belong to the Series, not a Stage row. The PropertyKey metadata names the field they land on.
    public static readonly Error FinaleCountNotPositive = Error.Validation(
        code: "Series.Finale.FinalistCount.NotPositive",
        description: "The Finale needs a whole number of finalists greater than zero. Remove the Finale if the Series has none.",
        metadata: new Dictionary<string, object> { { PropertyKey, "Series.Finale.FinalistCount" } });

    public static Error FinaleCountExceedsSurvivor(int count, int survivorCount) => Error.Validation(
        code: "Series.Finale.FinalistCount.ExceedsSurvivor",
        description: $"A Finale of {count} cannot be larger than the last Survivor Stage's {survivorCount}.",
        metadata: new Dictionary<string, object> { { PropertyKey, "Series.Finale.FinalistCount" } });

    public static Error StageNotBeforeFinale(DateOnly finaleDate, string tournament, DateOnly tournamentEndDate) => Error.Validation(
        code: "Series.Finale.Date.StageNotBefore",
        description: $"No Stage can come after the Finale, but {tournament} ends {Format(tournamentEndDate)} and the Finale is {Format(finaleDate)}.",
        metadata: new Dictionary<string, object> { { PropertyKey, "Series.Finale.Date" } });

    public static Error FinaleBowlingCenterNotAvailable(string bowlingCenterId) => Error.Validation(
        code: "Series.Finale.BowlingCenter.NotAvailable",
        description: "The selected Finale bowling center does not exist or is not open.",
        metadata: new Dictionary<string, object> { { PropertyKey, "Series.Finale.BowlingCenterId" }, { "BowlingCenterId", bowlingCenterId } });

    public static Error AdvancingCountRequired(int order, string tournament, StageKind kind)
        => Stage(order, tournament, "Series.Stage.AdvancingCountRequired", $"a {kind.Name} Stage needs an advancing count.", ErrorType.Validation);

    public static Error AdvancingCountNotPositive(int order, string tournament)
        => Stage(order, tournament, "Series.Stage.AdvancingCountNotPositive", "advancing count must be a whole number greater than zero.", ErrorType.Validation);

    public static Error SquadRequired(int order, string tournament)
        => Stage(order, tournament, "Series.Stage.SquadRequired", "a Survivor Stage needs a squad date and start time.", ErrorType.Validation);

    public static Error SquadNotAllowed(int order, string tournament, StageKind kind)
        => Stage(order, tournament, "Series.Stage.SquadNotAllowed", $"a {kind.Name} Stage has no designated squad.", ErrorType.Validation);

    public static Error SquadDateOutsideTournament(int order, string tournament, DateOnly squadDate, DateOnly startDate, DateOnly endDate)
        => Stage(order, tournament, "Series.Stage.SquadDateOutsideTournament", $"the squad date {Format(squadDate)} is outside the tournament's dates ({Format(startDate)} to {Format(endDate)}).", ErrorType.Validation);

    private static Error Stage(int order, string? tournament, string code, string reason, ErrorType type)
    {
        var label = tournament is null ? $"Stage {order}" : $"Stage {order} ({tournament})";
        var metadata = new Dictionary<string, object> { { StageOrderKey, order } };
        var description = $"{label}: {reason}";

        return type == ErrorType.Conflict
            ? Error.Conflict(code, description, metadata)
            : Error.Validation(code, description, metadata);
    }

    private static string Format(DateOnly date)
        => date.ToString("M/d/yyyy", CultureInfo.InvariantCulture);
}
