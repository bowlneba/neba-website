using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Sponsors;

namespace Neba.Api.Sponsors.GetSponsorDetail;

internal sealed class GetSponsorDetailEndpointSummary
    : Summary<GetSponsorDetailEndpoint>
{
    public GetSponsorDetailEndpointSummary()
    {
        Summary = "Gets sponsor detail by slug.";
        Description = "Retrieves detailed information for a specific sponsor, including contact details, business address, promotional content, and social media links.";

        Response(200, "The sponsor detail.",
            contentType: MediaTypeNames.Application.Json,
            example: new SponsorDetailResponse
            {
                Id = "01JWXYZEXAMPLE00000000000",
                Name = "Acme Bowling Supply",
                Slug = "acme-bowling-supply",
                IsCurrentSponsor = true,
                Priority = 1,
                Tier = "Gold",
                Category = "Equipment",
                TagPhrase = "The best in bowling",
                PhoneNumbers = [],
            });

        Response(404, "Sponsor not found.");
    }
}
