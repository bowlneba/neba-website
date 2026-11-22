using ErrorOr;

namespace Neba.Api.Features.Series.Domain;

internal static class SeriesAdvancementErrors
{
    public const string OfficialStageOrdersKey = "OfficialStageOrders";

    public static readonly Error VersionConflict = Error.Conflict(
        code: "Series.Version.Conflict",
        description: "This Series changed after you loaded it. Reload and try again.");

    public static readonly Error ConcurrentChange = Error.Conflict(
        code: "Series.ConcurrentChange",
        description: "Another change to this Series was saved first. Reload and try again.");

    public static readonly Error Busy = Error.Conflict(
        code: "Series.Advancement.Busy",
        description: "Advancement for this Series is running. Try again in a moment.");

    public static Error StageNotFound(SeriesStageId id) => Error.NotFound(
        code: "Series.Advancement.StageNotFound",
        description: "Stage not found.",
        metadata: new Dictionary<string, object> { { "StageId", id.ToString() } });

    public static readonly Error NotQualifyingStage = Error.Validation(
        code: "Series.Override.StageNotQualifying",
        description: "Overrides apply to Qualifying Stages only.");

    public static Error StageNotComputed(StageStatus status) => Error.Conflict(
        code: "Series.Override.StageNotComputed",
        description: status == StageStatus.Official
            ? "This Stage is official. Recompute it from the Series page before changing it."
            : "This Stage has not been computed yet.");

    public static readonly Error NoResultRow = Error.Validation(
        code: "Series.Override.NoResultRow",
        description: "The bowler has no result in this Stage's tournament.");

    public static readonly Error BowlerAlreadyInField = Error.Conflict(
        code: "Series.Override.BowlerAlreadyInField",
        description: "The bowler is already in the Series Field.");

    public static readonly Error OverrideExists = Error.Conflict(
        code: "Series.Override.Exists",
        description: "This bowler already has an override on this Stage. Remove it first.");

    public static readonly Error BowlerNotInSlot = Error.Validation(
        code: "Series.Override.BowlerNotInSlot",
        description: "The bowler does not hold a filled slot in this Stage.");

    public static readonly Error OverrideReasonRequired = Error.Validation(
        code: "Series.Override.Reason.Required",
        description: "A reason is required.");

    public static Error OverrideReasonTooLong(int maxLength) => Error.Validation(
        code: "Series.Override.Reason.TooLong",
        description: $"The reason must be {maxLength} characters or fewer.");

    public static Error ConfirmationRequired(IReadOnlyCollection<int> officialOrders) => Error.Conflict(
        code: "Series.Recompute.ConfirmationRequired",
        description: $"Stage {string.Join(", ", officialOrders)} {(officialOrders.Count == 1 ? "is" : "are")} official. Recomputing resets them to Computed. Confirm to continue.",
        metadata: new Dictionary<string, object> { { OfficialStageOrdersKey, officialOrders.ToArray() } });

    public static readonly Error StageNotSurvivor = Error.Validation(
        code: "Series.Squad.StageNotSurvivor",
        description: "Only a Survivor Stage has a squad.");

    public static readonly Error SquadNotInTournament = Error.Validation(
        code: "Series.Squad.NotInTournament",
        description: "The squad does not belong to this Stage's tournament.");

    // Invariants of the stored output types. Computed data should never trip these.
    public static readonly Error EntryInvalid = Error.Validation(
        code: "Series.FieldEntry.Invalid",
        description: "A Stage entry needs a place; a Backfill entry needs points above zero and no place.");

    public static readonly Error PositionInvalid = Error.Validation(
        code: "Series.Position.Invalid",
        description: "A position must be above zero.");

    public static readonly Error OutcomeInvalid = Error.Validation(
        code: "Series.SurvivorOutcome.Invalid",
        description: "A block total cannot be negative, and an advancing bowler needs one.");
}