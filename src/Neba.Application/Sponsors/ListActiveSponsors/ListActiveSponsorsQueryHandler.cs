using Neba.Application.Messaging;
using Neba.Application.Storage;

namespace Neba.Application.Sponsors.ListActiveSponsors;

internal sealed class ListActiveSponsorsQueryHandler(ISponsorQueries sponsorQueries, IFileStorageService fileStorageService)
    : IQueryHandler<ListActiveSponsorsQuery, IReadOnlyCollection<SponsorSummaryDto>>
{
    private readonly ISponsorQueries _sponsorQueries = sponsorQueries;
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public async Task<IReadOnlyCollection<SponsorSummaryDto>> HandleAsync(ListActiveSponsorsQuery query, CancellationToken cancellationToken)
    {
        var sponsors = await _sponsorQueries.GetActiveSponsorsAsync(cancellationToken);

        return [.. sponsors
            .Select(sponsor => sponsor.LogoContainer is not null && sponsor.LogoPath is not null
                ? sponsor with { LogoUrl = _fileStorageService.GetBlobUri(sponsor.LogoContainer, sponsor.LogoPath) }
                : sponsor)];
    }
}