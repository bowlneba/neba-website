using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Tournaments.EditTournamentLogo;

namespace Neba.Api.Features.Tournaments.EditTournamentLogo;

internal sealed class EditTournamentLogoRequestValidator
    : Validator<EditTournamentLogoRequest>
{
    public EditTournamentLogoRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty().WithErrorCode("EditTournamentLogoRequest.IdRequired").WithMessage("Id is required.")
            .Length(26).WithErrorCode("EditTournamentLogoRequest.IdInvalidLength").WithMessage("Id must be a 26-character ULID.");

        RuleFor(r => r.Input)
            .NotNull().WithErrorCode("EditTournamentLogoRequest.InputRequired").WithMessage("Input is required.");

        When(r => r.Input?.Logo is not null, () =>
        {
            RuleFor(r => r.Input.Logo!.Container)
                .NotEmpty().WithErrorCode("EditTournamentLogoRequest.LogoContainerRequired").WithMessage("Logo container is required.");

            RuleFor(r => r.Input.Logo!.Path)
                .NotEmpty().WithErrorCode("EditTournamentLogoRequest.LogoPathRequired").WithMessage("Logo path is required.");

            RuleFor(r => r.Input.Logo!.ContentType)
                .NotEmpty().WithErrorCode("EditTournamentLogoRequest.LogoContentTypeRequired").WithMessage("Logo content type is required.");

            RuleFor(r => r.Input.Logo!.SizeInBytes)
                .GreaterThan(0).WithErrorCode("EditTournamentLogoRequest.LogoSizeInvalid").WithMessage("Logo size must be greater than zero.");
        });
    }
}