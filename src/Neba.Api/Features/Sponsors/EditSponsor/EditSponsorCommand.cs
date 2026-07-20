using ErrorOr;

using Neba.Api.Contacts;
using Neba.Api.Contracts.Contact;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Sponsors.EditSponsor;

internal sealed record EditSponsorCommand
    : ICommand<Updated>
{
    public required SponsorId SponsorId { get; init; }

    public required string Name { get; init; }

    public required bool IsCurrentSponsor { get; init; }

    public required int Priority { get; init; }

    public required SponsorTier Tier { get; init; }

    public required SponsorCategory Category { get; init; }

    public StoredFile? Logo { get; init; }

    public Uri? WebsiteUrl { get; init; }

    public string? TagPhrase { get; init; }

    public string? Description { get; init; }

    public string? LiveReadText { get; init; }

    public string? PromotionalNotes { get; init; }

    public Uri? FacebookUrl { get; init; }

    public Uri? InstagramUrl { get; init; }

    public string? BusinessStreet { get; init; }

    public string? BusinessUnit { get; init; }

    public string? BusinessCity { get; init; }

    public UsState? BusinessState { get; init; }

    public string? BusinessPostalCode { get; init; }

    public string? BusinessEmailAddress { get; init; }

    public IReadOnlyCollection<PhoneNumberInput> PhoneNumbers { get; init; } = [];

    public string? ContactName { get; init; }

    public PhoneNumberType? ContactPhoneType { get; init; }

    public string? ContactPhoneNumber { get; init; }

    public string? ContactPhoneExtension { get; init; }

    public string? ContactEmail { get; init; }
}