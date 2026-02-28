using ErrorOr;

namespace Neba.Domain.BowlingCenters;

/// <summary>
/// Represents a contiguous block of usable tenpin lanes defined by a start lane number and an end lane number. Lanes are always used in consecutive pairs, and the PinFallType indicates the pinsetter mechanism (FreeFall or StringPin).
/// </summary>
public sealed record LaneRange
{
    /// <summary>
    /// The starting lane number of the range. Must be a positive odd number (e.g., 1, 3, 5, etc.) to ensure it represents the first lane in a pair.
    /// </summary>
    public required int StartLane { get; init; }

    /// <summary>
    /// The ending lane number of the range. Must be a positive even number (e.g., 2, 4, 6, etc.) and must be greater than the starting lane to ensure it represents the second lane in a pair.
    /// </summary>
    public required int EndLane { get; init; }

    /// <summary>
    /// The type of pinsetter mechanism used on the lanes in this range. This is required to determine the scoring rules and certification requirements for sanctioned play on these lanes.
    /// </summary>
    public required PinFallType PinFallType { get; init; }

    /// <summary>
    /// Calculates the number of lane pairs in this range. Since lanes are grouped in pairs (odd-even), the total number of lanes divided by 2 gives the number of pairs. For example, a range from lane 1 to lane 4 would have 2 pairs: (1,2) and (3,4).
    /// </summary>
    public int PairCount
        => (EndLane - StartLane + 1) / 2;

    /// <summary>
    /// Generates the pairs of lanes in this range as tuples of (odd lane, even lane). For example, if the range is from lane 1 to lane 4, this method would yield (1, 2) and (3, 4). This is useful for iterating over the pairs of lanes for scoring or other operations.
    /// </summary>
    /// <returns>An enumerable of tuples representing the pairs of lanes in this range.</returns>
    public IEnumerable<(int OddLane, int EvenLane)> LanePairs()
    {
        for (var i = 0; i < PairCount; i++)
        {
            yield return (StartLane + (i * 2), StartLane + (i * 2) + 1);
        }
    }

    /// <summary>
    /// Creates a LaneRange instance with validation. Ensures the starting lane is a positive odd number, the ending lane is a positive even number greater than the start, and the pin fall type is provided. Returns an ErrorOr&lt;LaneRange&gt; containing either a valid LaneRange or validation errors.
    /// </summary>
    /// <param name="startLane">The starting lane number (must be positive and odd).</param>
    /// <param name="endLane">The ending lane number (must be positive, even, and greater than startLane).</param>
    /// <param name="pinFallType">The pinsetter mechanism type (FreeFall or StringPin).</param>
    /// <returns>ErrorOr&lt;LaneRange&gt; containing a valid LaneRange or validation errors.</returns>
    public static ErrorOr<LaneRange> Create(int startLane, int endLane, PinFallType pinFallType)
    {
        if (pinFallType is null)
        {
            return LaneRangeErrors.PinFallTypeRequired;
        }

        if (startLane < 1 || startLane % 2 == 0)
        {
            return LaneRangeErrors.StartLaneMustBeOdd;
        }

        if (endLane < 1 || endLane % 2 != 0)
        {
            return LaneRangeErrors.EndLaneMustBeEven;
        }

        if (endLane <= startLane)
        {
            return LaneRangeErrors.EndLaneMustExceedStartLane;
        }

        return new LaneRange
        {
            StartLane = startLane,
            EndLane = endLane,
            PinFallType = pinFallType
        };
    }
}

internal static class LaneRangeErrors
{
    public static Error PinFallTypeRequired
        => Error.Validation("LaneRange.PinFallType.Required", "Pin fall type is required.");

    public static Error StartLaneMustBeOdd
        => Error.Validation("LaneRange.StartLane.MustBeOdd", "Start lane must be a positive odd number.");

    public static Error EndLaneMustBeEven
        => Error.Validation("LaneRange.EndLane.MustBeEven", "End lane must be a positive even number.");

    public static Error EndLaneMustExceedStartLane
        => Error.Validation("LaneRange.EndLane.MustExceedStartLane", "End lane must be greater than start lane.");
}