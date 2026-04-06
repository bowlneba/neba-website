using Microsoft.EntityFrameworkCore;

using Neba.Application.Sponsors;
using Neba.Domain.Sponsors;

namespace Neba.Infrastructure.Database.Queries;

internal sealed class SponsorQueries(AppDbContext appDbContext)
    : ISponsorQueries
{
    private readonly IQueryable<Sponsor> _sponsors = appDbContext.Sponsors.AsNoTracking();

    public async Task<IReadOnlyCollection<SponsorSummaryDto>> GetActiveSponsorsAsync(CancellationToken cancellationToken)
        => await _sponsors
            .Where(sponsor => sponsor.IsCurrentSponsor)
            .Select(sponsor => new SponsorSummaryDto
            {
                Name = sponsor.Name,
                Slug = sponsor.Slug,
                LogoContainer = sponsor.Logo != null ? sponsor.Logo.Container : null,
                LogoPath = sponsor.Logo != null ? sponsor.Logo.Path : null,
                IsCurrentSponsor = sponsor.IsCurrentSponsor,
                Priority = sponsor.Priority,
                Tier = sponsor.Tier,
                Category = sponsor.Category,
                TagPhrase = sponsor.TagPhrase,
                Description = sponsor.Description,
                WebsiteUrl = sponsor.WebsiteUrl,
                FacebookUrl = sponsor.FacebookUrl,
                InstagramUrl = sponsor.InstagramUrl
            })
            .ToListAsync(cancellationToken);
}