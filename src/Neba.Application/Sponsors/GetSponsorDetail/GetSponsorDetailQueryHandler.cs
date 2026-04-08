using ErrorOr;

using Neba.Application.Messaging;

namespace Neba.Application.Sponsors.GetSponsorDetail;

internal sealed class GetSponsorDetailQueryHandler(ISponsorQueries sponsorQueries)
    : IQueryHandler<GetSponsorDetailQuery, ErrorOr<SponsorDetailDto>>
{
    private readonly ISponsorQueries _sponsorQueries = sponsorQueries;

    public async Task<ErrorOr<SponsorDetailDto>> HandleAsync(GetSponsorDetailQuery query, CancellationToken cancellationToken)
    {
        var sponsor = await _sponsorQueries.GetSponsorAsync(query.Slug, cancellationToken);

        return sponsor is not null
            ? sponsor
            : SponsorErrors.SponsorNotFound(query.Slug);
    }
}