namespace Neba.Website.Server.HallOfFame;

/// <summary>
/// One bowler's row on the points page.
/// </summary>
public sealed record HallOfFamePointsStandingViewModel
{
    /// <summary>
    /// The bowler's ULID string.
    /// </summary>
    public required string BowlerId { get; init; }

    /// <summary>
    /// The bowler's name, "First Last".
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// Total Hall of Fame points earned.
    /// </summary>
    public required int TotalPoints { get; init; }

    /// <summary>
    /// Different seasons with a point-earning title or award.
    /// </summary>
    public required int SeasonsWithPoints { get; init; }

    /// <summary>
    /// Counted membership years.
    /// </summary>
    public required int MembershipYears { get; init; }

    /// <summary>
    /// False when the first membership is not on file, so the count may be low.
    /// </summary>
    public required bool MembershipHistoryComplete { get; init; }

    /// <summary>
    /// True when the bowler has the required points.
    /// </summary>
    public required bool MeetsPoints { get; init; }

    /// <summary>
    /// True when the bowler has the required seasons.
    /// </summary>
    public required bool MeetsSeasons { get; init; }

    /// <summary>
    /// True when the bowler has the required membership years.
    /// </summary>
    public required bool MeetsMembershipYears { get; init; }

    /// <summary>
    /// Seasons still needed; 0 when met.
    /// </summary>
    public required int SeasonsNeeded { get; init; }

    /// <summary>
    /// Membership years still needed; 0 when met.
    /// </summary>
    public required int MembershipYearsNeeded { get; init; }

    /// <summary>
    /// Where the dot sits on the 0 to 36 line, 0 to 100. Whole numbers, so no culture issue in CSS.
    /// </summary>
    public required int LinePercent { get; init; }

    /// <summary>
    /// "In the Hall of Fame for Meritorious Service"; null when the bowler is not inducted.
    /// </summary>
    public string? InductedLine { get; init; }
}