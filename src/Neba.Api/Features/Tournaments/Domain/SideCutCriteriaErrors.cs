using ErrorOr;

namespace Neba.Api.Features.Tournaments.Domain;

internal static class SideCutCriteriaErrors
{
    public static readonly Error BothAgesRequired
        = Error.Validation(
            code: "SideCutCriteria.BothAgesRequired",
            description: "At least one of minimum age or maximum age must be provided.");

    public static readonly Error MinimumAgeMustBeGreaterThanZero
        = Error.Validation(
            code: "SideCutCriteria.MinimumAgeInvalid",
            description: "Minimum age must be greater than zero.");

    public static readonly Error MaximumAgeMustBeGreaterThanZero
        = Error.Validation(
            code: "SideCutCriteria.MaximumAgeInvalid",
            description: "Maximum age must be greater than zero.");

    public static readonly Error AgeRangeInvalid
        = Error.Validation(
            code: "SideCutCriteria.AgeRangeInvalid",
            description: "Minimum age cannot be greater than maximum age.");
}