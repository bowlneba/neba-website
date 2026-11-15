using FluentValidation;

using Hangfire;

using Microsoft.AspNetCore.Mvc;

namespace Neba.Api.Legacy.BowlerMemberships;

internal static class NewBowlerMembershipEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapNewBowlerMembership()
        {
            app.MapPost("/bowler-memberships/new", (
                NewBowlerMembershipRequest request,
                [FromServices] IValidator<NewBowlerMembershipRequest> validator,
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

internal sealed record NewBowlerMembershipRequest(int BowlerMembershipId);

internal sealed class NewBowlerMembershipRequestValidator
    : AbstractValidator<NewBowlerMembershipRequest>
{
    public NewBowlerMembershipRequestValidator()
    {
        RuleFor(request => request.BowlerMembershipId)
            .GreaterThan(0);
    }
}