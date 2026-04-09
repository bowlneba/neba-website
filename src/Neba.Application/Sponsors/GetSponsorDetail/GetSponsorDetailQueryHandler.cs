using ErrorOr;

using Neba.Application.Messaging;
using Neba.Application.Storage;

namespace Neba.Application.Sponsors.GetSponsorDetail;

internal sealed class GetSponsorDetailQueryHandler(ISponsorQueries sponsorQueries, IFileStorageService fileStorageService)
    : IQueryHandler<GetSponsorDetailQuery, ErrorOr<SponsorDetailDto>>
{
    private readonly ISponsorQueries _sponsorQueries = sponsorQueries;
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public async Task<ErrorOr<SponsorDetailDto>> HandleAsync(GetSponsorDetailQuery query, CancellationToken cancellationToken)
    {
        var sponsor = await _sponsorQueries.GetSponsorAsync(query.Slug, cancellationToken);

        if (sponsor is null)
        {
            return SponsorErrors.SponsorNotFound(query.Slug);
        }

        if (sponsor.LogoContainer is not null && sponsor.LogoPath is not null)
        {
            return sponsor with { LogoUrl = _fileStorageService.GetBlobUri(sponsor.LogoContainer, sponsor.LogoPath) };
        }

        return sponsor;
    }
}