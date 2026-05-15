using ErrorOr;

using Neba.Domain.Bowlers;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Represents a single eligibility criterion within a Side Cut criteria group, such as an age range or gender requirement.
/// </summary>
public sealed class SideCutCriteria
{
    /// <summary>
    /// The criteria group to which this side cut criterion belongs.  This association is required for the criterion to be evaluated as part of the side cut's eligibility determination process.
    /// </summary>
    internal SideCutCriteriaGroup CriteriaGroup { get; init; } = null!;

    /// <summary>
    /// The minimum age requirement for the side cut, if applicable. A value of null indicates no minimum age requirement.
    /// </summary>
    public int? MinimumAge { get; }

    /// <summary>
    /// The maximum age requirement for the side cut, if applicable. A value of null indicates no maximum age requirement.
    /// </summary>
    public int? MaximumAge { get; }

    /// <summary>
    /// The gender requirement for the side cut, if applicable. A value of null indicates no gender requirement.
    /// </summary>
    public Gender? GenderRequirement { get; }

    private SideCutCriteria(int? minimumAge, int? maximumAge)
    {
        MinimumAge = minimumAge;
        MaximumAge = maximumAge;
    }

    private SideCutCriteria(Gender gender)
    {
        GenderRequirement = gender;
    }

    /// <summary>
    /// Creates a new age-based criterion. At least one age bound must be supplied, bounds must be non-negative, and the minimum cannot exceed the maximum.
    /// </summary>
    /// <param name="minimumAge">The minimum age requirement for the side cut, if applicable.</param>
    /// <param name="maximumAge">The maximum age requirement for the side cut, if applicable.</param>
    /// <returns>An ErrorOr&lt;SideCutCriteria&gt; representing the result of the creation attempt.</returns>
    internal static ErrorOr<SideCutCriteria> CreateAgeRequirement(int? minimumAge, int? maximumAge)
    {
        if (minimumAge == null && maximumAge == null)
        {
            return SideCutCriteriaErrors.BothAgesRequired;
        }

        if (minimumAge <= 0)
        {
            return SideCutCriteriaErrors.MinimumAgeMustBeGreaterThanZero;
        }

        if (maximumAge <= 0)
        {
            return SideCutCriteriaErrors.MaximumAgeMustBeGreaterThanZero;
        }

        return minimumAge != null && maximumAge != null && minimumAge > maximumAge
            ? SideCutCriteriaErrors.AgeRangeInvalid
            : new SideCutCriteria(minimumAge, maximumAge);
    }

    /// <summary>
    /// Creates a new gender-based criterion.
    /// </summary>
    /// <param name="gender">The gender requirement for the side cut.</param>
    /// <returns>An ErrorOr&lt;SideCutCriteria&gt; representing the result of the creation attempt.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the gender parameter is null.</exception>
    internal static ErrorOr<SideCutCriteria> CreateGenderRequirement(Gender gender)
    {
        ArgumentNullException.ThrowIfNull(gender);

        return new SideCutCriteria(gender);
    }
}

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