using Neba.Application.Messaging;

namespace Neba.Application.BowlingCenters.ListBowlingCenters;

internal sealed class ListBowlingCentersQueryHandler(IBowlingCenterQueries bowlingCenterQueries)
        : IQueryHandler<ListBowlingCentersQuery, IReadOnlyCollection<BowlingCenterSummaryDto>>
{
    private readonly IBowlingCenterQueries _bowlingCenterQueries = bowlingCenterQueries;

    public Task<IReadOnlyCollection<BowlingCenterSummaryDto>> HandleAsync(ListBowlingCentersQuery query, CancellationToken cancellationToken)
        => _bowlingCenterQueries.GetAllAsync(cancellationToken);
}