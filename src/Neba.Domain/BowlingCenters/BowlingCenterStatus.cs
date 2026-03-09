using Ardalis.SmartEnum;

namespace Neba.Domain.BowlingCenters;

/// <summary>
/// Represents the operational status of a bowling center, indicating whether it is currently open for business or has been closed. This status can be used to filter active bowling centers in the system and to manage the lifecycle of bowling center entities. The Open status indicates that the bowling center is operational and can host events, while the Closed status indicates that the bowling center has ceased operations and should not be considered for new events or bookings.
/// </summary>
public sealed class BowlingCenterStatus
    : SmartEnum<BowlingCenterStatus>
{
    /// <summary>
    /// Indicates that the bowling center is currently open and operational. Bowling centers with this status are active and can host events, leagues, and other activities. This status is typically assigned to bowling centers that are in good standing and have not been marked as closed due to business decisions, renovations, or other reasons.
    /// </summary>
    public static readonly BowlingCenterStatus Open = new(nameof(Open), 0);

    /// <summary>
    /// Indicates that the bowling center has been closed and is no longer operational. Bowling centers with this status are considered inactive and should not be included in searches for active venues or used for new event bookings. This status may be assigned to bowling centers that have permanently ceased operations, are undergoing long-term renovations, or have been closed for other reasons.
    /// </summary>
    public static readonly BowlingCenterStatus Closed = new(nameof(Closed), 1);

    private BowlingCenterStatus(string name, int value)
        : base(name, value)
    { }
}