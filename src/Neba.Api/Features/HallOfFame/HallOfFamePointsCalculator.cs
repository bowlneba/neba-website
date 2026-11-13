using Neba.Api.Contracts.Memberships;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.HallOfFame;

internal sealed record TitleWin(BowlerId BowlerId, SeasonId SeasonId, DateOnly StartDate, DateOnly EndDate, TournamentType TournamentType);

internal sealed record AwardWin(BowlerId BowlerId, SeasonId SeasonId, DateOnly SeasonEndDate, BowlerOfTheYearCategory Category);

internal sealed record MembershipRecord(BowlerId BowlerId, MembershipType Type, DateOnly EndDate);

/// <summary>
/// One point-earning title or award in a bowler's breakdown.
/// </summary>
/// <param name="SeasonId">The season it was earned in.</param>
/// <param name="Date">When it was earned.</param>
/// <param name="Source">The title or award name.</param>
/// <param name="Points">The points it earned.</param>
/// <param name="RunningTotal">The bowler's points after this entry, counting oldest first.</param>
/// <param name="ReachedThreshold">True only for the first entry that takes the running total to the required points.</param>
internal sealed record EarnedPoint(SeasonId SeasonId, DateOnly Date, string Source, int Points, int RunningTotal = 0, bool ReachedThreshold = false);

internal sealed record MembershipYear(int Year, MembershipType Type);

/// <summary>
/// A bowler's counted membership years, and whether we hold their whole membership history.
/// </summary>
/// <param name="Years">Counted years, oldest first.</param>
/// <param name="HistoryComplete">
/// False only when the bowler has counting memberships but no <see cref="MembershipType.New"/> record: New is
/// once per lifetime, so without it the bowler joined before our records begin and <paramref name="Years"/> can understate.
/// </param>
internal sealed record MembershipSummary(IReadOnlyList<MembershipYear> Years, bool HistoryComplete)
{
    /// <summary>For a bowler with no counting memberships: no years, and nothing to understate.</summary>
    public static MembershipSummary None { get; } = new([], true);
}

internal sealed record BowlerPoints(BowlerId BowlerId, int TotalPoints, int SeasonsWithPoints, IReadOnlyList<EarnedPoint> Breakdown)
{
    public bool MeetsPoints => TotalPoints >= HallOfFamePointRules.RequiredPoints;

    public bool MeetsSeasons => SeasonsWithPoints >= HallOfFamePointRules.RequiredSeasonsWithPoints;
}

internal interface IHallOfFamePointsCalculator
{
    /// <summary>Points per bowler, only bowlers with at least one point. Breakdown is oldest first.</summary>
    IReadOnlyCollection<BowlerPoints> CalculatePoints(IReadOnlyCollection<TitleWin> titles, IReadOnlyCollection<AwardWin> awards);

    /// <summary>
    /// Membership summary per bowler: counted years (one per calendar year of the end date, oldest first, only
    /// for types that count toward the Hall of Fame) and whether the history is complete (see
    /// <see cref="MembershipSummary"/>). Bowlers with no records at all are absent; use
    /// <see cref="MembershipSummary.None"/> for them.
    /// </summary>
    IReadOnlyDictionary<BowlerId, MembershipSummary> SummarizeMemberships(IReadOnlyCollection<MembershipRecord> memberships);
}

internal sealed class HallOfFamePointsCalculator
    : IHallOfFamePointsCalculator
{
    public IReadOnlyCollection<BowlerPoints> CalculatePoints(IReadOnlyCollection<TitleWin> titles, IReadOnlyCollection<AwardWin> awards)
    {
        ArgumentNullException.ThrowIfNull(titles);
        ArgumentNullException.ThrowIfNull(awards);

        var earned = titles
            .Select(title => (title.BowlerId, Rule: HallOfFamePointRules.ForTitle(title.TournamentType, title.StartDate), title.SeasonId, Date: title.EndDate))
            .Concat(awards
                .Select(award => (award.BowlerId, Rule: HallOfFamePointRules.ForAward(award.Category), award.SeasonId, Date: award.SeasonEndDate)))
            .Where(entry => entry.Rule is not null)
            .Select(entry => (entry.BowlerId, Point: new EarnedPoint(entry.SeasonId, entry.Date, entry.Rule!.Label, entry.Rule.Points)));

        return [.. earned
            .GroupBy(entry => entry.BowlerId)
            .Select(group =>
            {
                var breakdown = WithRunningTotals([.. group.Select(entry => entry.Point).OrderBy(point => point.Date)]);

                return new BowlerPoints(
                    group.Key,
                    breakdown.Sum(point => point.Points),
                    breakdown.Select(point => point.SeasonId).Distinct().Count(),
                    breakdown);
            })];
    }

    /// <summary>Adds the running total and marks the entry that first reaches the required points.</summary>
    private static List<EarnedPoint> WithRunningTotals(List<EarnedPoint> ordered)
    {
        var total = 0;
        var reached = false;

        return [.. ordered.Select(point =>
        {
            total += point.Points;
            var crossed = !reached && total >= HallOfFamePointRules.RequiredPoints;
            reached |= crossed;

            return point with { RunningTotal = total, ReachedThreshold = crossed };
        })];
    }

    public IReadOnlyDictionary<BowlerId, MembershipSummary> SummarizeMemberships(IReadOnlyCollection<MembershipRecord> memberships)
    {
        ArgumentNullException.ThrowIfNull(memberships);

        return memberships
            .GroupBy(membership => membership.BowlerId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var counting = group.Where(membership => membership.Type.CountsTowardHallOfFame).ToList();

                    var years = counting
                        .GroupBy(membership => membership.EndDate.Year)
                        .Select(year => new MembershipYear(year.Key, year.OrderBy(membership => membership.EndDate).First().Type))
                        .OrderBy(year => year.Year)
                        .ToList();

                    // Incomplete only when counting memberships exist without the lifetime-once New record.
                    var historyComplete = counting.Count == 0
                        || counting.Any(membership => membership.Type == MembershipType.New);

                    return new MembershipSummary(years, historyComplete);
                });
    }
}