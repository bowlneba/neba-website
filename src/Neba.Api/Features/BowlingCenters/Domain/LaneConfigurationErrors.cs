using ErrorOr;

namespace Neba.Api.Features.BowlingCenters.Domain;

internal static class LaneConfigurationErrors
{
    public static Error RangesRequired
        => Error.Validation("LaneConfiguration.Ranges.Required", "Lane configuration must contain at least one lane range.");

    public static Error RangesOverlapping
        => Error.Validation("LaneConfiguration.Ranges.Overlapping", "Lane ranges must not overlap.");

    public static Error RangesAdjacent
        => Error.Validation("LaneConfiguration.Ranges.Adjacent", "Adjacent lane ranges of the same pin fall type must be merged into a single range.");
}