using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Sponsors.CreateSponsor;

namespace Neba.Api.Features.Sponsors.CreateSponsor;

internal sealed class CreateSponsorSummary : Summary<CreateSponsorEndpoint>
{
    public CreateSponsorSummary()
    {
        Summary = "Creates a sponsor.";
        Description = "Creates a sponsor with its full field set (tier, category, contact, business address, etc). Slug is derived from the name unless a staff-supplied override is given; either way it is normalized and must be unique. Requires the Sponsors.CreateSponsor permission.";

#pragma warning disable S1075 // URIs should not be hardcoded
        Response(201, "Sponsor created.",
            contentType: MediaTypeNames.Application.Json,
            example: new SponsorResponse
            {
                SponsorId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E",
                Slug = "storm-products-inc"
            });
#pragma warning restore S1075 // URIs should not be hardcoded

        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Sponsors.CreateSponsor permission.");
        Response(409, "Slug already taken, or the Title Sponsor tier is already assigned to another sponsor.");
        Response(422, "Name, slug, tier, category, or a contact/address/email/phone field failed a domain validation rule.");
    }
}
