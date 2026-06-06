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
        var rows = await _sponsors
            .Where(sponsor => sponsor.IsCurrentSponsor)
            .Select(sponsor => new
            {
                sponsor.Name,
                sponsor.Slug,
                LogoContainer = sponsor.Logo != null ? sponsor.Logo.Container : null,
                LogoPath = sponsor.Logo != null ? sponsor.Logo.Path : null,
                sponsor.IsCurrentSponsor,
                sponsor.Priority,
                Tier = sponsor.Tier.Name,
                Category = sponsor.Category.Name,
                sponsor.TagPhrase,
                sponsor.Description,
                sponsor.WebsiteUrl,
                sponsor.FacebookUrl,
                sponsor.InstagramUrl
            })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new SponsorSummaryDto
        {
            Name = row.Name,
            Slug = row.Slug,
            LogoUrl = row.LogoContainer is not null && row.LogoPath is not null
                ? _fileStorageService.GetBlobUri(row.LogoContainer, row.LogoPath)
                : null,
            IsCurrentSponsor = row.IsCurrentSponsor,
            Priority = row.Priority,
            Tier = row.Tier,
            Category = row.Category,
            TagPhrase = row.TagPhrase,
            Description = row.Description,
            WebsiteUrl = row.WebsiteUrl,
            FacebookUrl = row.FacebookUrl,
            InstagramUrl = row.InstagramUrl
        })];
    }
}