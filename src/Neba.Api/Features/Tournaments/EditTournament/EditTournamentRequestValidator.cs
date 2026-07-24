using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Tournaments.EditTournament;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments.EditTournament;

internal sealed class EditTournamentRequestValidator
    : Validator<EditTournamentRequest>
{
    public EditTournamentRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty().WithErrorCode("EditTournamentRequest.IdRequired").WithMessage("Id is required.")
            .Length(26).WithErrorCode("EditTournamentRequest.IdInvalidLength").WithMessage("Id must be a 26-character ULID.");

        RuleFor(r => r.Tournament.Name)
            .NotEmpty().WithErrorCode("EditTournamentRequest.NameRequired").WithMessage("Name is required.")
            .MaximumLength(127).WithErrorCode("EditTournamentRequest.NameTooLong").WithMessage("Name must be 127 characters or fewer.");

        RuleFor(r => r.Tournament.TournamentType)
            .NotEmpty().WithErrorCode("EditTournamentRequest.TournamentTypeRequired").WithMessage("Tournament type is required.")
            .Must(t => TournamentType.List.Any(known => known.Name == t))
            .WithErrorCode("EditTournamentRequest.TournamentTypeInvalid")
            .WithMessage("Tournament type must be a known, active format.");

        RuleFor(r => r.Tournament.EndDate)
            .GreaterThanOrEqualTo(r => r.Tournament.StartDate)
            .WithErrorCode("EditTournamentRequest.EndDateBeforeStartDate")
            .WithMessage("End date must not be before start date.");

        RuleFor(r => r.Tournament.EntryFee)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("EditTournamentRequest.EntryFeeInvalid")
            .WithMessage("Entry fee must not be negative.");

        RuleFor(r => r.Tournament.NebaAddedMoney)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("EditTournamentRequest.NebaAddedMoneyInvalid")
            .WithMessage("NEBA added money must not be negative.");

        RuleFor(r => r.Tournament.ExternalRegistrationUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode("EditTournamentRequest.ExternalRegistrationUrlInvalid")
            .WithMessage("External registration URL must be an absolute URL.")
            .When(r => r.Tournament.ExternalRegistrationUrl is not null);

        RuleFor(r => r.Tournament.PatternLengthCategory)
            .Must(c => PatternLengthCategory.List.Any(known => known.Name == c))
            .WithErrorCode("EditTournamentRequest.PatternLengthCategoryInvalid")
            .WithMessage("Pattern length category must be one of: Short, Medium, Long.")
            .When(r => !string.IsNullOrWhiteSpace(r.Tournament.PatternLengthCategory));

        RuleFor(r => r.Tournament.PatternRatioCategory)
            .Must(c => PatternRatioCategory.List.Any(known => known.Name == c))
            .WithErrorCode("EditTournamentRequest.PatternRatioCategoryInvalid")
            .WithMessage("Pattern ratio category must be one of: Sport, Challenge, Recreation.")
            .When(r => !string.IsNullOrWhiteSpace(r.Tournament.PatternRatioCategory));

        RuleFor(r => r.Tournament)
            .Must(t => string.IsNullOrWhiteSpace(t.OilPatternId)
                || (string.IsNullOrWhiteSpace(t.PatternLengthCategory) && string.IsNullOrWhiteSpace(t.PatternRatioCategory)))
            .WithErrorCode("EditTournamentRequest.OilPatternAndManualCategoriesConflict")
            .WithMessage("Provide either an oil pattern ID or manual pattern categories, not both.");
    }
}
