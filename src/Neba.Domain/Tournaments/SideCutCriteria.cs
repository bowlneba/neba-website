using ErrorOr;

using Neba.Domain.Bowlers;

namespace Neba.Domain.Tournaments;

/// <summary>
/// Represents the criteria for a side cut in a tournament, such
/// </summary>
public sealed class SideCutCriteria
{
    internal SideCutCriteriaGroup CriteriaGroup { get; init; } = null!;

    /// <summary>
    /// The minimum age requirement for the side cut, if applicable. A value of null indicates no minimum age requirement.
    /// </summary>
    public int? MinimumAge { get; private set; }

    /// <summary>
    /// The maximum age requirement for the side cut, if applicable. A value of null indicates no maximum age requirement.
    /// </summary>
    public int? MaximumAge { get; private set; }

    /// <summary>
    /// The gender requirement for the side cut, if applicable. A value of null indicates no gender requirement.
    /// </summary>
    public Gender? GenderRequirement { get; private set; }

    /// <summary>
    /// Creates a new instance of SideCutCriteria with the specified age requirements. At least one of minimumAge or maximumAge must be provided, and both must be greater than or equal to zero if provided. Additionally, if both minimumAge and maximumAge are provided, minimumAge cannot be greater than maximumAge. This method returns an ErrorOr&lt;SideCutCriteria&gt; to handle validation errors gracefully.
    /// </summary>
    /// <param name="minimumAge">The minimum age requirement for the side cut, if applicable.</param>
    /// <param name="maximumAge">The maximum age requirement for the side cut, if applicable.</param>
    /// <returns>An ErrorOr&lt;SideCutCriteria&gt; representing the result of the creation attempt.</returns>
    /// <exception cref="ArgumentNullException">Thrown if both minimumAge and maximumAge are null.</exception>
    public static ErrorOr<SideCutCriteria> CreateAgeRequirement(int? minimumAge, int? maximumAge)
    {
        if (minimumAge == null && maximumAge == null)
        {
            throw new ArgumentNullException(nameof(minimumAge), "At least one of minimumAge or maximumAge must be provided.");
        }

        if (minimumAge < 0)
        {
            return SideCutCriteriaErrors.MinimumAgeMustBeGreaterThanZero;
        }

        if (maximumAge < 0)
        {
            return SideCutCriteriaErrors.MaximumAgeMustBeGreaterThanZero;
        }

        return minimumAge != null && maximumAge != null && minimumAge > maximumAge
            ? SideCutCriteriaErrors.AgeRangeInvalid
            : new SideCutCriteria
            {
                MinimumAge = minimumAge,
                MaximumAge = maximumAge
            };
    }

    /// <summary>
    /// Creates a new instance of SideCutCriteria with the specified gender requirement. The gender parameter must not be null. This method returns an ErrorOr&lt;SideCutCriteria&gt; to handle validation errors gracefully.
    /// </summary>
    /// <param name="gender">The gender requirement for the side cut.</param>
    /// <returns>An ErrorOr&lt;SideCutCriteria&gt; representing the result of the creation attempt.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the gender parameter is null.</exception>
    public static ErrorOr<SideCutCriteria> CreateGenderRequirement(Gender gender)
    {
        ArgumentNullException.ThrowIfNull(gender);

        return new SideCutCriteria
        {
            GenderRequirement = gender
        };
    }
}

internal static class SideCutCriteriaErrors
{
    public static readonly Error MinimumAgeMustBeGreaterThanZero
        = Error.Validation(
            code: "SideCutCriteria.MinimumAgeInvalid",
            description: "Minimum age must be greater than or equal to zero.");

    public static readonly Error MaximumAgeMustBeGreaterThanZero
        = Error.Validation(
            code: "SideCutCriteria.MaximumAgeInvalid",
            description: "Maximum age must be greater than or equal to zero.");

    public static readonly Error AgeRangeInvalid
        = Error.Validation(
            code: "SideCutCriteria.AgeRangeInvalid",
            description: "Minimum age cannot be greater than maximum age.");
}