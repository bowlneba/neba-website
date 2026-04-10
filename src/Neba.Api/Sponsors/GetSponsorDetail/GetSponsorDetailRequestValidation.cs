using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Sponsors.GetSponsorDetail;

internal sealed class GetSponsorDetailRequestValidation
    : Validator<GetSponsorDetailRequest>
{
    public GetSponsorDetailRequestValidation()
    {
        RuleFor(request => request.Slug)
            .NotEmpty()
            .WithErrorCode("SponsorDetailRequest.SlugRequired")
            .WithMessage("Sponsor slug is required.");
    }
}