namespace Neba.Api.Features.HallOfFame.ListHallOfFamePoints;

/// <summary>
/// One bowler's Hall of Fame points and how they measure against the Superior Performance criteria.
/// </summary>
public sealed record HallOfFamePointsStandingDto
{
    /// <summary>
    /// The bowler's ULID string.
    /// </summary>
    public required string BowlerId { get; init; }

    /// <summary>
    /// The bowler's name, formatted as "First Last".
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// Total Hall of Fame points earned.
    /// </summary>
    public required int TotalPoints { get; init; }

    /// <summary>
    /// Different seasons with at least one point-earning title or award.
    /// </summary>
    public required int SeasonsWithPoints { get; init; }

    /// <summary>
    /// Counted membership years since the membership data start year.
    /// </summary>
    public required int MembershipYears { get; init; }

    /// <summary>
    /// False only when the bowler has counting memberships but no New membership on file: the bowler joined
    /// before the records begin and the count can understate. True otherwise, including no counting memberships.
    /// </summary>
    public required bool MembershipHistoryComplete { get; init; }

    /// <summary>
    /// True when points, seasons and membership years all meet the Superior Performance criteria.
    /// </summary>
    public required bool Eligible { get; init; }

    /// <summary>
    /// True when the bowler has the required points.
    /// </summary>
    public required bool MeetsPoints { get; init; }

    /// <summary>
    /// True when the bowler has titles or awards in the required number of seasons.
    /// </summary>
    public required bool MeetsSeasons { get; init; }

    /// <summary>
    /// Category names of an induction for something other than Superior Performance; empty if none.
    /// </summary>
    public required IReadOnlyCollection<string> InductedCategories { get; init; }
}