using FastEndpoints;

namespace Neba.Api.Features.Sponsors.EditSponsor;

internal sealed class EditSponsorSummary : Summary<EditSponsorEndpoint>
{
    public EditSponsorSummary()
    {
        Summary = "Edits a sponsor.";
        Description = "Replaces the sponsor's editable fields (tier, category, contact, business address, logo, etc). The slug is immutable and is not part of this request. Phone numbers are a full replace-set. Requires the Sponsors.EditSponsor permission.";

        Response(204, "Sponsor updated.");
        Response(400, "Id, Name, Tier, Category, or a contact/URL field failed structural validation (e.g. missing, too long, or not a well-formed URL).");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Sponsors.EditSponsor permission.");
        Response(404, "No sponsor exists with the given Id.");
        Response(409, "The Title Sponsor tier is already assigned to another sponsor.");
        Response(422, "Name, business address, email, or phone number failed a domain validation rule.");
    }
}
