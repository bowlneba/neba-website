using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.OilPatterns.CreateOilPattern;

namespace Neba.Api.Features.Tournaments.CreateOilPattern;

internal sealed class CreateOilPatternRequestValidator
    : Validator<CreateOilPatternRequest>
{
    public CreateOilPatternRequestValidator()
    {
        RuleFor(r => r.Name)
            .NotEmpty().WithErrorCode("CreateOilPatternRequest.NameRequired").WithMessage("Name is required.")
            .MaximumLength(63).WithErrorCode("CreateOilPatternRequest.NameTooLong").WithMessage("Name must be 63 characters or fewer.");

        RuleFor(r => r.Length)
            .GreaterThan(0).WithErrorCode("CreateOilPatternRequest.LengthMustBePositive").WithMessage("Length must be greater than zero.");

        RuleFor(r => r.Volume)
            .GreaterThan(0).WithErrorCode("CreateOilPatternRequest.VolumeMustBePositive").WithMessage("Volume must be greater than zero.");

        RuleFor(r => r.LeftRatio)
            .GreaterThanOrEqualTo(0).WithErrorCode("CreateOilPatternRequest.LeftRatioInvalid").WithMessage("Left ratio must not be negative.");

        RuleFor(r => r.RightRatio)
            .GreaterThanOrEqualTo(0).WithErrorCode("CreateOilPatternRequest.RightRatioInvalid").WithMessage("Right ratio must not be negative.");
    }
}