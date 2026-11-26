using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Series.MakeStageOfficial;

namespace Neba.Api.Features.Series.MakeStageOfficial;

internal sealed class MakeStageOfficialRequestValidator : Validator<MakeStageOfficialRequest>
{
    public MakeStageOfficialRequestValidator()
    {
        RuleFor(r => r.Id).IsUlid("Series ID", "MakeStageOfficialRequest.Id");
        RuleFor(r => r.StageId).IsUlid("Stage ID", "MakeStageOfficialRequest.StageId");

        RuleFor(r => r.Official).NotNull()
            .WithErrorCode("MakeStageOfficialRequest.OfficialRequired").WithMessage("Official is required.");

        RuleFor(r => r.Official.Version).GreaterThanOrEqualTo(0)
            .WithErrorCode("MakeStageOfficialRequest.VersionInvalid").WithMessage("Version must be zero or more.")
            .When(r => r.Official is not null);
    }
}