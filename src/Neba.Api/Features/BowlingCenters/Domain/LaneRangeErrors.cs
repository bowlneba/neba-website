using ErrorOr;

namespace Neba.Api.Features.BowlingCenters.Domain;

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