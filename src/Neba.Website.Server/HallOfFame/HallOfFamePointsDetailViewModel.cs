namespace Neba.Website.Server.HallOfFame;

/// <summary>
/// One bowler's point breakdown and counted membership years.
/// </summary>
public sealed record HallOfFamePointsDetailViewModel
{
    /// <summary>
    /// The bowler's name, "First Last".
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// "In the Hall of Fame for ..."; null when the bowler is not inducted.
    /// </summary>
    public string? InductedLine { get; init; }

    /// <summary>
    /// Total Hall of Fame points earned.
    /// </summary>
    public required int TotalPoints { get; init; }

    /// <summary>
    /// Different seasons with a point-earning title or award.
    /// </summary>
    public required int SeasonsWithPoints { get; init; }

    /// <summary>
    /// False when the first membership is not on file, so the years may understate.
    /// </summary>
    public required bool MembershipHistoryComplete { get; init; }

    /// <summary>
    /// Each point-earning title or award, oldest first.
    /// </summary>
    public required IReadOnlyCollection<HallOfFamePointsEntryViewModel> Breakdown { get; init; }

    /// <summary>
    /// Each counted membership year, oldest first.
    /// </summary>
    public required IReadOnlyCollection<HallOfFameMembershipYearViewModel> MembershipYears { get; init; }
}