using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.HallOfFame.GetHallOfFamePointsDetail;

internal sealed class GetHallOfFamePointsDetailQueryHandler(
    HallOfFamePointsReader reader,
    IHallOfFamePointsCalculator calculator)
        : IQueryHandler<GetHallOfFamePointsDetailQuery, ErrorOr<HallOfFamePointsDetailDto>>
{
    public async Task<ErrorOr<HallOfFamePointsDetailDto>> HandleAsync(GetHallOfFamePointsDetailQuery query, CancellationToken cancellationToken)
    {
        BowlerId[] ids = [query.BowlerId];

        var names = await reader.LoadNamesAsync(ids, cancellationToken);
        if (!names.TryGetValue(query.BowlerId, out var name))
        {
            return BowlerErrors.NotFound(query.BowlerId);
        }

        var titles = await reader.LoadTitleWinsAsync(query.BowlerId, cancellationToken);
        var awards = await reader.LoadAwardWinsAsync(query.BowlerId, cancellationToken);
        var inducted = await reader.LoadInductedCategoriesAsync(cancellationToken);
        var memberships = calculator
            .SummarizeMemberships(await reader.LoadMembershipsAsync(ids, cancellationToken))
            .GetValueOrDefault(query.BowlerId, MembershipSummary.None);

        var points = calculator.CalculatePoints(titles, awards).SingleOrDefault();

        return new HallOfFamePointsDetailDto
        {
            BowlerId = query.BowlerId.ToString(),
            BowlerName = name,
            InductedCategories = inducted.TryGetValue(query.BowlerId, out var categories)
                ? [.. categories.Select(category => category.Name)]
                : [],
            TotalPoints = points?.TotalPoints ?? 0,
            SeasonsWithPoints = points?.SeasonsWithPoints ?? 0,
            Breakdown = [.. (points?.Breakdown ?? []).Select(entry => new HallOfFamePointsEntryDto
            {
                Source = entry.Source,
                Date = entry.Date,
                Points = entry.Points
            })],
            MembershipYears = [.. memberships.Years
                .Select(year => new HallOfFameMembershipYearDto { Year = year.Year, MembershipType = year.Type.Name })],
            MembershipHistoryComplete = memberships.HistoryComplete
        };
    }
}
