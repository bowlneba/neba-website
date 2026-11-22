using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Series;
using Neba.Api.Contracts.Series.CreateStageOverride;
using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Series.CreateStageOverride;

internal sealed class CreateStageOverrideRequestValidator : Validator<CreateStageOverrideRequest>
{
    public CreateStageOverrideRequestValidator()
    {
        RuleFor(r => r.Id).IsUlid("Series ID", "CreateStageOverrideRequest.Id");
        RuleFor(r => r.StageId).IsUlid("Stage ID", "CreateStageOverrideRequest.StageId");
        RuleFor(r => r.StageOverride.BowlerId).IsUlid("Bowler ID", "CreateStageOverrideRequest.BowlerId");

        RuleFor(r => r.StageOverride.Kind)
            .Must(kind => kind is OverrideKindValues.Add or OverrideKindValues.Clear)
            .WithErrorCode("CreateStageOverrideRequest.KindInvalid").WithMessage("Kind must be Add (1) or Clear (2).");

        RuleFor(r => r.StageOverride.Reason)
            .NotEmpty().WithErrorCode("CreateStageOverrideRequest.ReasonRequired").WithMessage("A reason is required.")
            .MaximumLength(StageOverride.ReasonMaxLength).WithErrorCode("CreateStageOverrideRequest.ReasonTooLong")
            .WithMessage($"The reason must be {StageOverride.ReasonMaxLength} characters or fewer.");

        RuleFor(r => r.StageOverride.Version).GreaterThanOrEqualTo(0)
            .WithErrorCode("CreateStageOverrideRequest.VersionInvalid").WithMessage("Version must be zero or more.");
    }
}