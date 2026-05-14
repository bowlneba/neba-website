using Neba.Domain.Tournaments;

namespace Neba.Application.Tournaments;

/// <summary>
/// Data transfer object representing a bowling oil pattern, used for transferring oil pattern information between application layers.
/// </summary>
public sealed record OilPatternDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the oil pattern.
    /// </summary>
    public required OilPatternId Id { get; init; }

    /// <summary>
    /// Gets or sets the name of the oil pattern.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the length of the oil pattern in feet.
    /// </summary>
    public required int Length { get; init; }

    /// <summary>
    /// Gets or sets the volume of oil applied to the pattern in milliliters.
    /// This value can affect how the bowling ball interacts with the lane, influencing factors such as ball speed and hook potential.
    /// Higher volumes typically create more oil on the lane, which can lead to less friction and a longer skid phase for the bowling ball.
    /// Conversely, lower volumes may result in more friction and an earlier hook phase.
    /// Understanding the volume of oil is important for bowlers to adjust their strategies and equipment choices accordingly.
    /// See <see cref="PatternRatioCategory"/> for more information on pattern ratio categories, which also impact ball behavior on the lane.
    /// </summary>
    public required decimal Volume { get; init; }

    /// <summary>
    /// The ratio of the average oil volume on the inner lane boards (boards L18–R18) to the average oil volume on the left outside boards (boards L3–L7).
    /// </summary>
    public required decimal LeftRatio { get; init; }

    /// <summary>
    /// The ratio of the average oil volume on the inner lane boards (boards L18–R18) to the average oil volume on the right outside boards (boards R3–R7).
    /// </summary>
    public required decimal RightRatio { get; init; }

    /// <summary>
    /// The optional GUID identifying this pattern in the Kegel public pattern library.
    /// </summary>
    public required Guid? KegelId { get; init; }
}