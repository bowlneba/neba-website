namespace Neba.Website.Server.HallOfFame;

/// <summary>
/// How <see cref="HallOfFamePointsDetailView"/> lays out one bowler's points.
/// </summary>
public enum HallOfFamePointsDetailLayout
{
    /// <summary>The points-page modal: left-aligned stats, a points table with running totals, and the threshold highlight.</summary>
    Standing,

    /// <summary>The inductee profile: centered stats, and plain two-column lists with no point values.</summary>
    Profile
}