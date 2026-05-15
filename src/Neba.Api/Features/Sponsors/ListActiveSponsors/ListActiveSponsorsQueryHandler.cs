using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Messaging;
using Neba.Api.Storage;

namespace Neba.Api.Features.Sponsors.ListActiveSponsors;

internal sealed class ListActiveSponsorsQueryHandler(AppDbContext appDbContext, IFileStorageService fileStorageService)
    : IQueryHandler<ListActiveSponsorsQuery, IReadOnlyCollection<SponsorSummaryDto>>
{
    private readonly IQueryable<Sponsor> _sponsors = appDbContext.Sponsors.AsNoTracking();
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public async Task<IReadOnlyCollection<SponsorSummaryDto>> HandleAsync(ListActiveSponsorsQuery query, CancellationToken cancellationToken)
    {
        var sponsors = await _sponsors
            .Where(sponsor => sponsor.IsCurrentSponsor)
            .Select(sponsor => new SponsorSummaryDto
            {
                Name = sponsor.Name,
                Slug = sponsor.Slug,
                LogoContainer = sponsor.Logo != null ? sponsor.Logo.Container : null,
                LogoPath = sponsor.Logo != null ? sponsor.Logo.Path : null,
                IsCurrentSponsor = sponsor.IsCurrentSponsor,
                Priority = sponsor.Priority,
                Tier = sponsor.Tier.Name,
                Category = sponsor.Category.Name,
                TagPhrase = sponsor.TagPhrase,
                Description = sponsor.Description,
                WebsiteUrl = sponsor.WebsiteUrl,
                FacebookUrl = sponsor.FacebookUrl,
                InstagramUrl = sponsor.InstagramUrl
            })
            .ToListAsync(cancellationToken);

        return [.. sponsors
            .Select(sponsor => sponsor.LogoContainer is not null && sponsor.LogoPath is not null
                ? sponsor with { LogoUrl = _fileStorageService.GetBlobUri(sponsor.LogoContainer, sponsor.LogoPath) }
                : sponsor)];
    }
}