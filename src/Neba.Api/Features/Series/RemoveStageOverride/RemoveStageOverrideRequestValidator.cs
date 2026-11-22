using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.Series.RemoveStageOverride;

internal sealed class RemoveStageOverrideRequestValidator : Validator<RemoveStageOverrideRequest>
{
    public RemoveStageOverrideRequestValidator()
    {
        RuleFor(r => r.Id).IsUlid("Series ID", "RemoveStageOverrideRequest.Id");
        RuleFor(r => r.StageId).IsUlid("Stage ID", "RemoveStageOverrideRequest.StageId");
        RuleFor(r => r.OverrideId).IsUlid("Override ID", "RemoveStageOverrideRequest.OverrideId");

        RuleFor(r => r.Version)
            .NotNull().WithErrorCode("RemoveStageOverrideRequest.VersionRequired").WithMessage("Version is required.")
            .GreaterThanOrEqualTo(0).WithErrorCode("RemoveStageOverrideRequest.VersionInvalid").WithMessage("Version must be zero or more.");
    }
}