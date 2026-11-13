using Neba.Api.Contracts.OpenApi;

namespace Neba.Api.Contracts.HallOfFame.GetHallOfFamePointsDetail;

/// <summary>
/// One bowler's Hall of Fame points: the point breakdown and the counted membership years.
/// </summary>
public sealed record HallOfFamePointsDetailResponse
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
    /// Names of every induction category the bowler holds; empty if not inducted.
    /// </summary>
    [OpenApiSmartEnum("HallOfFameCategory")]
    public required IReadOnlyCollection<string> InductedCategories { get; init; }

    /// <summary>
    /// Total Hall of Fame points earned.
    /// </summary>
    public required int TotalPoints { get; init; }

    /// <summary>
    /// Different seasons with at least one point-earning title or award.
    /// </summary>
    public required int SeasonsWithPoints { get; init; }

    /// <summary>
    /// Each point-earning title or award, oldest first.
    /// </summary>
    public required IReadOnlyCollection<HallOfFamePointsEntryResponse> Breakdown { get; init; }

    /// <summary>
    /// Each counted membership year with the type behind it, oldest first.
    /// </summary>
    public required IReadOnlyCollection<HallOfFameMembershipYearResponse> MembershipYears { get; init; }

    /// <summary>
    /// False only when counting memberships exist without a New record, so the membership years can understate.
    /// </summary>
    public required bool MembershipHistoryComplete { get; init; }
}