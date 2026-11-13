namespace Neba.Api.Features.HallOfFame.ListHallOfFamePoints;

/// <summary>
/// The Hall of Fame point table, thresholds, and each bowler's standing.
/// </summary>
public sealed record HallOfFamePointsDto
{
    /// <summary>
    /// Every line of the point table, in display order.
    /// </summary>
    public required IReadOnlyCollection<HallOfFamePointRuleDto> Rules { get; init; }

    /// <summary>
    /// Points needed for Superior Performance.
    /// </summary>
    public required int RequiredPoints { get; init; }

    /// <summary>
    /// Different seasons with a point-earning title or award needed.
    /// </summary>
    public required int RequiredSeasonsWithPoints { get; init; }

    /// <summary>
    /// Membership years needed.
    /// </summary>
    public required int RequiredMembershipYears { get; init; }

    /// <summary>
    /// First year with membership records; earlier years are not counted.
    /// </summary>
    public required int MembershipDataStartYear { get; init; }

    /// <summary>
    /// Bowlers with points who are not inducted for Superior Performance, points high to low, then name.
    /// </summary>
    public required IReadOnlyCollection<HallOfFamePointsStandingDto> Standings { get; init; }
}