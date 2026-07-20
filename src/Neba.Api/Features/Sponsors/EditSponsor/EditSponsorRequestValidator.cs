using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Sponsors.EditSponsor;

namespace Neba.Api.Features.Sponsors.EditSponsor;

internal sealed class EditSponsorRequestValidator
    : Validator<EditSponsorRequest>
{
    public EditSponsorRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty()
            .WithErrorCode("EditSponsorRequest.IdRequired")
            .WithMessage("Id is required.")
            .Length(26)
            .WithErrorCode("EditSponsorRequest.IdInvalidLength")
            .WithMessage("Id must be a 26-character ULID.");

        this.ApplySponsorInputRules(
            r => r.Sponsor.Name,
            r => r.Sponsor.Tier,
            r => r.Sponsor.Category,
            r => r.Sponsor.WebsiteUrl,
            r => r.Sponsor.FacebookUrl,
            r => r.Sponsor.InstagramUrl,
            r => r.Sponsor.Contact,
            "EditSponsorRequest");
    }
}