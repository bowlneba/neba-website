using Ardalis.SmartEnum;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Represents the length category of the oil pattern applied to tournament lanes, expressed
/// as a range of feet. Pattern length describes how far down the lane oil is applied and
/// is one of two dimensions used to characterize tournament lane conditions.
/// Inherits from <see cref="SmartEnum{PatternLengthCategory}"/> for enhanced enum-like behavior.
/// </summary>
public sealed class PatternLengthCategory
    : SmartEnum<PatternLengthCategory>
{
    /// <summary>
    /// Short pattern: oil applied to 37 feet or less.
    /// </summary>
    public static readonly PatternLengthCategory ShortPattern = new("Short", 1, null, 37);

    /// <summary>
    /// Medium pattern: oil applied between 38 and 42 feet.
    /// </summary>
    public static readonly PatternLengthCategory MediumPattern = new("Medium", 2, 38, 42);

    /// <summary>
    /// Long pattern: oil applied to 43 feet or more.
    /// </summary>
    public static readonly PatternLengthCategory LongPattern = new("Long", 3, 43, null);

    private PatternLengthCategory(
        string name,
        int value,
        int? minimumLength,
        int? maximumLength)
        : base(name, value)
    {
        MinimumLength = minimumLength;
        MaximumLength = maximumLength;
    }

    /// <summary>
    /// Gets the minimum oil application length in feet for this category,
    /// or <see langword="null"/> for the shortest category where no lower bound applies.
    /// </summary>
    /// <value>The minimum length in feet, or <see langword="null"/> if not applicable.</value>
    public int? MinimumLength { get; }

    /// <summary>
    /// Gets the maximum oil application length in feet for this category,
    /// or <see langword="null"/> for the longest category where no upper bound applies.
    /// </summary>
    /// <value>The maximum length in feet, or <see langword="null"/> if not applicable.</value>
    public int? MaximumLength { get; }
}