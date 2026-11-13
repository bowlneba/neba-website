namespace Neba.Website.Server.HallOfFame;

/// <summary>
/// The thresholds the page and modal compare against.
/// </summary>
public sealed record HallOfFamePointRequirements
{
    /// <summary>
    /// Points needed for Superior Performance.
    /// </summary>
    public required int Points { get; init; }

    /// <summary>
    /// Different seasons with a point-earning title or award needed.
    /// </summary>
    public required int Seasons { get; init; }

    /// <summary>
    /// Membership years needed.
    /// </summary>
    public required int MembershipYears { get; init; }

    /// <summary>
    /// First year with membership records.
    /// </summary>
    public required int MembershipDataStartYear { get; init; }
}