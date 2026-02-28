using ErrorOr;

namespace Neba.Domain.BowlingCenters;

/// <summary>
/// Represents the overall configuration of lanes in a bowling center, consisting of one or more LaneRange instances. This class provides methods to calculate the total number of lane pairs and to validate that the lane ranges do not overlap or have adjacent ranges with the same pin fall type, which would indicate they should be merged. The equality implementation ensures that two LaneConfiguration instances are considered equal if their lane ranges are identical in sequence and content.
/// </summary>
public sealed record LaneConfiguration
{
    /// <summary>
    /// A read-only collection of LaneRange instances that define the contiguous blocks of usable lanes in the bowling center. Each LaneRange must be valid and the collection must not contain overlapping or adjacent ranges with the same pin fall type. This property is required to ensure that the lane configuration is properly defined and can be used for scoring and certification purposes.
    /// </summary>
    public required IReadOnlyCollection<LaneRange> Ranges { get; init; }

    /// <summary>
    /// Calculates the total number of lane pairs across all lane ranges in this configuration. This is done by summing the PairCount of each LaneRange in the Ranges collection. The total pair count is essential for determining the capacity of the bowling center and for scoring purposes, as it indicates how many pairs of lanes are available for play.
    /// </summary>
    public int TotalPairCount
        => Ranges.Sum(r => r.PairCount);

    /// <summary>
    /// Creates a LaneConfiguration instance with validation. Ensures that the provided collection of LaneRange instances is not null or empty, that the ranges do not overlap, and that adjacent ranges with the same pin fall type are not allowed (they should be merged instead). Returns an ErrorOr&lt;LaneConfiguration&gt; containing either a valid LaneConfiguration or validation errors if any of the conditions are not met.
    /// </summary>
    /// <param name="ranges"></param>
    /// <returns></returns>
    public static ErrorOr<LaneConfiguration> Create(IReadOnlyCollection<LaneRange> ranges)
    {
        if (ranges is null || ranges.Count == 0)
        {
            return LaneConfigurationErrors.RangesRequired;
        }

        var sorted = ranges.OrderBy(range => range.StartLane).ToList();

        for (var i = 0; i < sorted.Count - 1; i++)
        {
            var current = sorted[i];
            var next = sorted[i + 1];

            if (current.EndLane >= next.StartLane)
            {
                return LaneConfigurationErrors.RangesOverlapping;
            }

            if (current.EndLane + 1 == next.StartLane && current.PinFallType == next.PinFallType)
            {
                return LaneConfigurationErrors.RangesAdjacent;
            }
        }

        return new LaneConfiguration { Ranges = sorted.AsReadOnly() };
    }

    /// <inheritdoc/>
    public bool Equals(LaneConfiguration? other)
        => other is not null && Ranges.SequenceEqual(other.Ranges);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var range in Ranges)
        {
            hash.Add(range);
        }

        return hash.ToHashCode();
    }
}


internal static class LaneConfigurationErrors
{
    public static Error RangesRequired
        => Error.Validation("LaneConfiguration.Ranges.Required", "Lane configuration must contain at least one lane range.");

    public static Error RangesOverlapping
        => Error.Validation("LaneConfiguration.Ranges.Overlapping", "Lane ranges must not overlap.");

    public static Error RangesAdjacent
        => Error.Validation("LaneConfiguration.Ranges.Adjacent", "Adjacent lane ranges of the same pin fall type must be merged into a single range.");
}