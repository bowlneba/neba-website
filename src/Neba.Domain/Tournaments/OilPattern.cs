namespace Neba.Domain.Tournaments;

/// <summary>
/// Represents a bowling oil pattern, including its name, length, volume, and ratio information.
/// </summary>
public sealed class OilPattern
{
    /// <summary>
    /// Gets or sets the unique identifier for the oil pattern.
    /// </summary>
    public required OilPatternId Id { get; init; }

    /// <summary>
    /// Gets or sets the name of the oil pattern.
    /// This is a required property that should be set when creating an instance of <see cref="OilPattern"/>.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the length of the oil pattern in feet.
    /// This is a required property that should be set when creating an instance of <see cref="OilPattern"/>.
    /// The length of the pattern is typically categorized into short, medium, or long based on its value.
    /// For example, a pattern of 37 feet or less is considered short, 38 to 42 feet is medium, and 43 feet or more is long.
    /// See <see cref="PatternLengthCategory"/> for more information on pattern length categories.
    /// </summary>
    public required int Length { get; init; }

    /// <summary>
    /// Gets or sets the volume of oil applied to the pattern in milliliters.
    /// This is a required property that should be set when creating an instance of <see cref="OilPattern"/>.
    /// The volume of oil can affect how the bowling ball interacts with the lane, influencing factors such as ball speed and hook potential.
    /// Higher volumes typically create more oil on the lane, which can lead to less friction and a longer skid phase for the bowling ball.
    /// Conversely, lower volumes may result in more friction and an earlier hook phase.
    /// Understanding the volume of oil is important for bowlers to adjust their strategies and equipment choices accordingly.
    /// See <see cref="PatternRatioCategory"/> for more information on pattern ratio categories, which also impact ball behavior on the lane.
    /// </summary>
    public required decimal Volume { get; init; }

    /// <summary>
    /// The ratio of the average oil volume on the inner lane boards (boards L18–R18) to the average oil volume on the left outside boards (boards L3–L7).
    /// Expressed as the multiplier X in an X:1 ratio — e.g., a value of 3.5 means the center carries 3.5 times more oil than the left outside.
    /// Higher values indicate a more defined oil wall and an easier-playing pattern. A symmetric pattern will have equal left and right ratios.
    /// See <see cref="PatternRatioCategory"/> for USBC Sport Bowling thresholds.
    /// </summary>
    public decimal LeftRatio { get; init; }

    /// <summary>
    /// The ratio of the average oil volume on the inner lane boards (boards L18–R18) to the average oil volume on the right outside boards (boards R3–R7).
    /// Expressed as the multiplier X in an X:1 ratio — e.g., a value of 3.5 means the center carries 3.5 times more oil than the right outside.
    /// Higher values indicate a more defined oil wall and an easier-playing pattern. A symmetric pattern will have equal left and right ratios.
    /// See <see cref="PatternRatioCategory"/> for USBC Sport Bowling thresholds.
    /// </summary>
    public decimal RightRatio { get; init; }

    /// <summary>
    /// The optional GUID identifying this pattern in the Kegel public pattern library.
    /// When set, this value corresponds directly to the pattern's unique identifier in the Kegel system and can be used to reference the full pattern details at <c>patternlibrary.kegel.net</c>.
    /// Null for custom patterns not derived from the Kegel catalog.
    /// </summary>
    public Guid? KegelId { get; init; }
}