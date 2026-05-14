using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Sponsors;
using Neba.Domain.Sponsors;

namespace Neba.Api.Features.Sponsors.ListActiveSponsors;

internal sealed class ListActiveSponsorsEndpointSummary
    : Summary<ListActiveSponsorsEndpoint>
{
    public ListActiveSponsorsEndpointSummary()
    {
        Summary = "Lists all active sponsors.";
        Description = "Retrieves a summary list of all currently active sponsors, including tier, category, and social media links.";

        Response(200, "The list of active sponsors.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<SponsorSummaryResponse>
            {
                Items =
                [
                    new SponsorSummaryResponse
                    {
                        Name = "Acme Bowling Supply",
                        Slug = "acme-bowling-supply",
                        LogoUrl = null,
                        IsCurrentSponsor = true,
                        Priority = 1,
                        Tier = SponsorTier.Premier.Name,
                        Category = SponsorCategory.ProShop.Name,
                        TagPhrase = "The best in bowling",
                        Description = null,
                        WebsiteUrl = null,
                        FacebookUrl = null,
                        InstagramUrl = null,
                    }
                ],
            });
    }
}