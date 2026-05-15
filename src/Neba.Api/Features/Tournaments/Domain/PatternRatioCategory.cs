using Ardalis.SmartEnum;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Represents the oil-to-dry ratio category of the lane condition used in a tournament.
/// The ratio describes the relative difficulty of the lane condition and is one of two
/// dimensions used to characterize tournament lane conditions alongside pattern length.
/// Inherits from <see cref="SmartEnum{PatternRatioCategory}"/> for enhanced enum-like behavior.
/// </summary>
public sealed class PatternRatioCategory
    : SmartEnum<PatternRatioCategory>
{
    /// <summary>
    /// Sport condition: oil-to-dry ratio less than 4.0. The most challenging lane condition,
    /// consistent with USBC Sport designation.
    /// </summary>
    public static readonly PatternRatioCategory Sport = new(nameof(Sport), 1, null, 4m);

    /// <summary>
    /// Challenge condition: oil-to-dry ratio between 4.0 and 8.0. An intermediate lane condition,
    /// consistent with USBC Challenge designation.
    /// </summary>
    public static readonly PatternRatioCategory Challenge = new(nameof(Challenge), 2, 4m, 8m);

    /// <summary>
    /// Recreation condition: oil-to-dry ratio of 8.0 or greater. The most scoring-friendly
    /// lane condition, equivalent to a standard or house condition.
    /// </summary>
    public static readonly PatternRatioCategory Recreation = new(nameof(Recreation), 3, 8m, null);

    private PatternRatioCategory(
        string name,
        int value,
        decimal? minimumRatio,
        decimal? maximumRatio)
        : base(name, value)
    {
        MinimumRatio = minimumRatio;
        MaximumRatio = maximumRatio;
    }

    /// <summary>
    /// Gets the minimum oil-to-dry ratio for this category,
    /// or <see langword="null"/> for the Sport category where no lower bound applies.
    /// </summary>
    /// <value>The minimum ratio, or <see langword="null"/> if not applicable.</value>
    public decimal? MinimumRatio { get; }

    /// <summary>
    /// Gets the maximum oil-to-dry ratio for this category,
    /// or <see langword="null"/> for the Recreation category where no upper bound applies.
    /// </summary>
    /// <value>The maximum ratio, or <see langword="null"/> if not applicable.</value>
    public decimal? MaximumRatio { get; }
}