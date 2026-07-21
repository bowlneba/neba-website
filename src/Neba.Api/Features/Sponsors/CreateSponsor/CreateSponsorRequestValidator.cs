using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Sponsors.CreateSponsor;

namespace Neba.Api.Features.Sponsors.CreateSponsor;

internal sealed class CreateSponsorRequestValidator
    : Validator<CreateSponsorRequest>
{
    public CreateSponsorRequestValidator()
    {
        this.ApplySponsorInputRules(
            r => r.Sponsor.Name,
            r => r.Sponsor.Tier,
            r => r.Sponsor.Category,
            r => r.Sponsor.WebsiteUrl,
            r => r.Sponsor.FacebookUrl,
            r => r.Sponsor.InstagramUrl,
            r => r.Sponsor.Contact,
            "CreateSponsorRequest");

        RuleFor(r => r.Sponsor.Slug)
            .MaximumLength(63)
            .WithErrorCode("CreateSponsorRequest.SlugTooLong")
            .WithMessage("Slug must be 63 characters or fewer.")
            .When(r => !string.IsNullOrWhiteSpace(r.Sponsor.Slug));
    }
}