using FluentValidation;

using Hangfire;

using Microsoft.AspNetCore.Mvc;

namespace Neba.Api.Legacy.BowlerMemberships;

internal static class UpdateBowlerMembershipEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapUpdateBowlerMembership()
        {
            app.MapPost("/bowler-memberships/update", (
                UpdateBowlerMembershipRequest request,
                [FromServices] IValidator<UpdateBowlerMembershipRequest> validator,
                [FromServices] IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                // Delayed so NewBowlerSyncJob normally creates the bowler first; see BowlerMembershipSyncJob.BowlerSettleDelay.
                jobs.Schedule<BowlerMembershipSyncJob>(
                    job => job.UpsertAsync(request.BowlerMembershipId, CancellationToken.None),
                    BowlerMembershipSyncJob.BowlerSettleDelay);

                return Results.Accepted();
            });
        }
    }
}

internal sealed record UpdateBowlerMembershipRequest(int BowlerMembershipId);

internal sealed class UpdateBowlerMembershipRequestValidator
    : AbstractValidator<UpdateBowlerMembershipRequest>
{
    public UpdateBowlerMembershipRequestValidator()
    {
        RuleFor(request => request.BowlerMembershipId)
            .GreaterThan(0);
    }
}