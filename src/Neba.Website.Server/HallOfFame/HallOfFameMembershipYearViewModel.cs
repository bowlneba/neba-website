namespace Neba.Website.Server.HallOfFame;

/// <summary>
/// A counted membership year.
/// </summary>
public sealed record HallOfFameMembershipYearViewModel
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
