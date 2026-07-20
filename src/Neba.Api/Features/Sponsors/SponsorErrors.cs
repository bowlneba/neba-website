using ErrorOr;

namespace Neba.Api.Features.Sponsors;

internal static class SponsorErrors
{
    public static Error SponsorNotFound(string slug)
        => Error.NotFound(
            code: "Sponsor.NotFound",
            description: "Sponsor not found.",
            metadata: new()
            {
                {"slug", slug}
            });

    public static Error NameRequired
        => Error.Validation("Sponsor.Name.Required", "Name must not be empty.");

    public static Error SlugInvalid
        => Error.Validation("Sponsor.Slug.Invalid", "Slug must contain at least one alphanumeric character.");

    public static Error SlugReserved
        => Error.Validation("Sponsor.Slug.Reserved", "Slug 'new' is reserved for the sponsor-creation route.");

    public static Error SlugAlreadyExists(string slug)
        => Error.Conflict(
            code: "Sponsor.Slug.AlreadyExists",
            description: "A sponsor with this slug already exists.",
            metadata: new Dictionary<string, object> { { "Slug", slug } });

    public static Error TitleSponsorshipUnavailable
        => Error.Conflict(
            code: "Sponsor.TitleSponsor.Unavailable",
            description: "The Title Sponsor tier is already assigned to another sponsor.");

    public static Error BusinessAddressStateRequired
        => Error.Validation("Sponsor.BusinessAddress.StateRequired", "State is required when a business street address is supplied.");

    public static Error ContactPhoneTypeRequired
        => Error.Validation("Sponsor.Contact.PhoneTypeRequired", "Contact phone type is required when contact name, phone, or email is supplied.");
}