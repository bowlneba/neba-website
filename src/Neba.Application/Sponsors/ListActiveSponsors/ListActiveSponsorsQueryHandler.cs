using Neba.Application.Messaging;

namespace Neba.Application.Sponsors.ListActiveSponsors;

internal sealed class ListActiveSponsorsQueryHandler(ISponsorQueries sponsorQueries)
    : IQueryHandler<ListActiveSponsorsQuery, IReadOnlyCollection<SponsorSummaryDto>>
{
    public async Task<IReadOnlyCollection<SponsorSummaryDto>> HandleAsync(ListActiveSponsorsQuery query, CancellationToken cancellationToken)
    {
        var results = await sponsorQueries.GetActiveSponsorsAsync(cancellationToken);

        return results;
    }
}