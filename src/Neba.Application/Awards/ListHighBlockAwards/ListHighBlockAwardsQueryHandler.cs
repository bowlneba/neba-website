using Neba.Application.Messaging;

namespace Neba.Application.Awards.ListHighBlockAwards;

internal sealed class ListHighBlockAwardsQueryHandler(IAwardQueries awardQueries)
        : IQueryHandler<ListHighBlockAwardsQuery, IReadOnlyCollection<HighBlockAwardDto>>
{
    private readonly IAwardQueries _awardQueries = awardQueries;

    public Task<IReadOnlyCollection<HighBlockAwardDto>> HandleAsync(ListHighBlockAwardsQuery query, CancellationToken cancellationToken)
        => _awardQueries.GetAllHighBlockAwardsAsync(cancellationToken);
}