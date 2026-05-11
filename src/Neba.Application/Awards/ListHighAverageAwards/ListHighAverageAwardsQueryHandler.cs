using Neba.Application.Messaging;

namespace Neba.Application.Awards.ListHighAverageAwards;

internal sealed class ListHighAverageAwardsQueryHandler(IAwardQueries awardQueries)
    : IQueryHandler<ListHighAverageAwardsQuery, IReadOnlyCollection<HighAverageAwardDto>>
{
    private readonly IAwardQueries _awardQueries = awardQueries;

    public Task<IReadOnlyCollection<HighAverageAwardDto>> HandleAsync(ListHighAverageAwardsQuery query, CancellationToken cancellationToken)
        => _awardQueries.GetAllHighAverageAwardsAsync(cancellationToken);
}