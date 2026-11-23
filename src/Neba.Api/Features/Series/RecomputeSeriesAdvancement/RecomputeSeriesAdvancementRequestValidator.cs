using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Series.RecomputeSeriesAdvancement;

namespace Neba.Api.Features.Series.RecomputeSeriesAdvancement;

internal sealed class RecomputeSeriesAdvancementRequestValidator : Validator<RecomputeSeriesAdvancementRequest>
{
    public RecomputeSeriesAdvancementRequestValidator()
    {
        RuleFor(r => r.Id).IsUlid("Series ID", "RecomputeSeriesAdvancementRequest.Id");
        RuleFor(r => r.StageId).IsUlid("Stage ID", "RecomputeSeriesAdvancementRequest.StageId");

        RuleFor(r => r.Recomputation.Version).GreaterThanOrEqualTo(0)
            .WithErrorCode("RecomputeSeriesAdvancementRequest.VersionInvalid").WithMessage("Version must be zero or more.");
    }
}