using Neba.Application.Messaging;

namespace Neba.Application.Awards.ListHighAverageAwards;

internal sealed class ListHighAverageAwardsQueryHandler(IAwardQueries awardQueries)
    : IQueryHandler<ListHighAverageAwardsQuery, IReadOnlyCollection<HighAverageAwardDto>>
{
    private readonly IAwardQueries _awardQueries = awardQueries;

    public async Task<IReadOnlyCollection<HighAverageAwardDto>> HandleAsync(ListHighAverageAwardsQuery query, CancellationToken cancellationToken)
    {
        var awards = await _awardQueries.GetAllHighAverageAwardsAsync(cancellationToken);

        return awards;
    }
}