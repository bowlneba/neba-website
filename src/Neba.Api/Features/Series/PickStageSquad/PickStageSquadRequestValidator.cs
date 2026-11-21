using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Series.PickStageSquad;

namespace Neba.Api.Features.Series.PickStageSquad;

internal sealed class PickStageSquadRequestValidator : Validator<PickStageSquadRequest>
{
    public PickStageSquadRequestValidator()
    {
        RuleFor(r => r.Id).IsUlid("Series ID", "PickStageSquadRequest.Id");
        RuleFor(r => r.StageId).IsUlid("Stage ID", "PickStageSquadRequest.StageId");
        RuleFor(r => r.Squad.SquadId).IsUlid("Squad ID", "PickStageSquadRequest.SquadId");

        RuleFor(r => r.Squad.Version).GreaterThanOrEqualTo(0)
            .WithErrorCode("PickStageSquadRequest.VersionInvalid").WithMessage("Version must be zero or more.");
    }
}