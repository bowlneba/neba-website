namespace Neba.Api.Contracts.HallOfFame.GetHallOfFamePointsDetail;

/// <summary>
/// A calendar year that counts toward the Hall of Fame membership-years criterion.
/// </summary>
public sealed record HallOfFameMembershipYearResponse
{
    /// <summary>
    /// The calendar year.
    /// </summary>
    public required int Year { get; init; }

    /// <summary>
    /// Display name of the membership type.
    /// </summary>
    public required string MembershipType { get; init; }
}
