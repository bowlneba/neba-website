using System.Globalization;
using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Sponsors;
using Neba.Api.Features.Sponsors.Domain;

namespace Neba.Api.Features.Sponsors.GetSponsorDetail;

internal sealed class GetSponsorDetailEndpointSummary
    : Summary<GetSponsorDetailEndpoint>
{
    public GetSponsorDetailEndpointSummary()
    {
        Summary = "Gets sponsor detail by slug.";
        Description = "Retrieves detailed information for a specific sponsor, including contact details, business address, promotional content, and social media links.";

        Response(200, "The sponsor detail. Fields documented as sponsor-management-only (logo storage details, live read text, promotional notes, contact) are omitted for callers without that permission.",
            contentType: MediaTypeNames.Application.Json,
            example: new SponsorDetailResponse
            {
                Id = Ulid.Parse("01JWXYZEXAMPLE000000000000", CultureInfo.InvariantCulture),
                Name = "Acme Bowling Supply",
                Slug = "acme-bowling-supply",
                IsCurrentSponsor = true,
                Priority = 1,
                Tier = SponsorTier.Premier.Name,
                Category = SponsorCategory.Manufacturer.Name,
                TagPhrase = "The best in bowling",
                PhoneNumbers = [],
                TournamentsSponsored = [],
            });

        Response(404, "Sponsor not found.");
    }
}