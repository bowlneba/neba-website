using Neba.Application.Messaging;

namespace Neba.Application.Awards.ListBowlerOfTheYearAwards;

internal sealed class ListBowlerOfTheYearAwardsQueryHandler(IAwardQueries awardQueries)
    : IQueryHandler<ListBowlerOfTheYearAwardsQuery, IReadOnlyCollection<BowlerOfTheYearAwardDto>>
{
    private readonly IAwardQueries _awardQueries = awardQueries;

    public async Task<IReadOnlyCollection<BowlerOfTheYearAwardDto>> HandleAsync(
        ListBowlerOfTheYearAwardsQuery query, CancellationToken cancellationToken)
    {
        var awards = await _awardQueries.GetAllBowlerOfTheYearAwardsAsync(cancellationToken);

        return awards;
    }
}
