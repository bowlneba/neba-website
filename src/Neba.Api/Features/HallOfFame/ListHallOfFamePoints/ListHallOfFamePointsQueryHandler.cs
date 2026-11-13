using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.HallOfFame.ListHallOfFamePoints;

internal sealed class ListHallOfFamePointsQueryHandler(
    HallOfFamePointsReader reader,
    IHallOfFamePointsCalculator calculator)
        : IQueryHandler<ListHallOfFamePointsQuery, HallOfFamePointsDto>
{
    public async Task<HallOfFamePointsDto> HandleAsync(ListHallOfFamePointsQuery query, CancellationToken cancellationToken)
    {
        var titles = await reader.LoadTitleWinsAsync(null, cancellationToken);
        var awards = await reader.LoadAwardWinsAsync(null, cancellationToken);
        var inducted = await reader.LoadInductedCategoriesAsync(cancellationToken);

        var points = calculator.CalculatePoints(titles, awards)
            .Where(bowler => !(inducted.TryGetValue(bowler.BowlerId, out var categories)
                && categories.Contains(HallOfFameCategory.SuperiorPerformance)))
            .ToList();

        var bowlerIds = points.Select(bowler => bowler.BowlerId).ToList();
        var names = await reader.LoadNamesAsync(bowlerIds, cancellationToken);
        var memberships = calculator.SummarizeMemberships(await reader.LoadMembershipsAsync(bowlerIds, cancellationToken));

        // Points high to low, then name. There is no rank: the Hall of Fame is not a standings race.
        var standings = points
            .OrderByDescending(bowler => bowler.TotalPoints)
            .ThenBy(bowler => names[bowler.BowlerId], StringComparer.OrdinalIgnoreCase)
            .Select(bowler =>
            {
                var membership = memberships.GetValueOrDefault(bowler.BowlerId, MembershipSummary.None);

                return new HallOfFamePointsStandingDto
                {
                    BowlerId = bowler.BowlerId.ToString(),
                    BowlerName = names[bowler.BowlerId],
                    TotalPoints = bowler.TotalPoints,
                    SeasonsWithPoints = bowler.SeasonsWithPoints,
                    MembershipYears = membership.Years.Count,
                    MembershipHistoryComplete = membership.HistoryComplete,
                    Eligible = HallOfFamePointRules.MeetsAllCriteria(bowler.TotalPoints, bowler.SeasonsWithPoints, membership.Years.Count),
                    MeetsPoints = bowler.MeetsPoints,
                    MeetsSeasons = bowler.MeetsSeasons,
                    InductedCategories = inducted.TryGetValue(bowler.BowlerId, out var other)
                        ? [.. other.Select(category => category.Name)]
                        : []
                };
            })
            .ToList();

        return new HallOfFamePointsDto
        {
            Rules = [.. HallOfFamePointRules.All.Select(rule => new HallOfFamePointRuleDto
            {
                Group = rule.Group,
                Label = rule.Label,
                Points = rule.Points
            })],
            RequiredPoints = HallOfFamePointRules.RequiredPoints,
            RequiredSeasonsWithPoints = HallOfFamePointRules.RequiredSeasonsWithPoints,
            RequiredMembershipYears = HallOfFamePointRules.RequiredMembershipYears,
            MembershipDataStartYear = HallOfFamePointRules.MembershipDataStartYear,
            Standings = standings
        };
    }
}