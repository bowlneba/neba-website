using FluentValidation;

using Neba.Api.Contracts.Tournaments.CreateTournament;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments;

/// <summary>
/// Validation rules for <see cref="TournamentInput"/>, shared by every request that embeds it
/// (create, edit). The error code prefix keeps each caller's error codes namespaced to its own
/// request type (e.g. "CreateTournamentRequest.NameRequired"), matching the convention used
/// elsewhere in this API even though the rules themselves are identical.
/// </summary>
internal sealed class TournamentInputValidator : AbstractValidator<TournamentInput>
{
    public TournamentInputValidator(string errorCodePrefix = "TournamentInput")
    {
        RuleFor(t => t.Name)
            .NotEmpty().WithErrorCode($"{errorCodePrefix}.NameRequired").WithMessage("Name is required.")
            .MaximumLength(127).WithErrorCode($"{errorCodePrefix}.NameTooLong").WithMessage("Name must be 127 characters or fewer.");

        RuleFor(t => t.TournamentType)
            .NotEmpty().WithErrorCode($"{errorCodePrefix}.TournamentTypeRequired").WithMessage("Tournament type is required.")
            .Must(t => TournamentType.List.Any(known => known.Name == t))
            .WithErrorCode($"{errorCodePrefix}.TournamentTypeInvalid")
            .WithMessage("Tournament type must be a known, active format.");

        RuleFor(t => t.EndDate)
            .GreaterThanOrEqualTo(t => t.StartDate)
            .WithErrorCode($"{errorCodePrefix}.EndDateBeforeStartDate")
            .WithMessage("End date must not be before start date.");

        RuleFor(t => t.EntryFee)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode($"{errorCodePrefix}.EntryFeeInvalid")
            .WithMessage("Entry fee must not be negative.");

        RuleFor(t => t.NebaAddedMoney)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode($"{errorCodePrefix}.NebaAddedMoneyInvalid")
            .WithMessage("NEBA added money must not be negative.");

        RuleFor(t => t.ExternalRegistrationUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode($"{errorCodePrefix}.ExternalRegistrationUrlInvalid")
            .WithMessage("External registration URL must be an absolute URL.")
            .When(t => t.ExternalRegistrationUrl is not null);

        RuleFor(t => t.PatternLengthCategory)
            .Must(c => PatternLengthCategory.List.Any(known => known.Name == c))
            .WithErrorCode($"{errorCodePrefix}.PatternLengthCategoryInvalid")
            .WithMessage("Pattern length category must be one of: Short, Medium, Long.")
            .When(t => !string.IsNullOrWhiteSpace(t.PatternLengthCategory));

        RuleFor(t => t.PatternRatioCategory)
            .Must(c => PatternRatioCategory.List.Any(known => known.Name == c))
            .WithErrorCode($"{errorCodePrefix}.PatternRatioCategoryInvalid")
            .WithMessage("Pattern ratio category must be one of: Sport, Challenge, Recreation.")
            .When(t => !string.IsNullOrWhiteSpace(t.PatternRatioCategory));

        RuleFor(t => t)
            .Must(t => string.IsNullOrWhiteSpace(t.OilPatternId)
                || (string.IsNullOrWhiteSpace(t.PatternLengthCategory) && string.IsNullOrWhiteSpace(t.PatternRatioCategory)))
            .WithErrorCode($"{errorCodePrefix}.OilPatternAndManualCategoriesConflict")
            .WithMessage("Provide either an oil pattern ID or manual pattern categories, not both.");
    }
}