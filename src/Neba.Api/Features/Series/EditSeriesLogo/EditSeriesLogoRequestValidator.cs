using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Series.EditSeriesLogo;

namespace Neba.Api.Features.Series.EditSeriesLogo;

internal sealed class EditSeriesLogoRequestValidator
    : Validator<EditSeriesLogoRequest>
{
    public EditSeriesLogoRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty().WithErrorCode("EditSeriesLogoRequest.IdRequired").WithMessage("Id is required.")
            .Length(26).WithErrorCode("EditSeriesLogoRequest.IdInvalidLength").WithMessage("Id must be a 26-character ULID.");

        RuleFor(r => r.Input)
            .NotNull().WithErrorCode("EditSeriesLogoRequest.InputRequired").WithMessage("Input is required.");

        When(r => r.Input?.Logo is not null, () =>
        {
            RuleFor(r => r.Input.Logo!.Container)
                .NotEmpty().WithErrorCode("EditSeriesLogoRequest.LogoContainerRequired").WithMessage("Logo container is required.");

            RuleFor(r => r.Input.Logo!.Path)
                .NotEmpty().WithErrorCode("EditSeriesLogoRequest.LogoPathRequired").WithMessage("Logo path is required.");

            RuleFor(r => r.Input.Logo!.ContentType)
                .NotEmpty().WithErrorCode("EditSeriesLogoRequest.LogoContentTypeRequired").WithMessage("Logo content type is required.");

            RuleFor(r => r.Input.Logo!.SizeInBytes)
                .GreaterThan(0).WithErrorCode("EditSeriesLogoRequest.LogoSizeInvalid").WithMessage("Logo size must be greater than zero.");
        });
    }
}
