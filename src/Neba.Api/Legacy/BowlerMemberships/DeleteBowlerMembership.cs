using FluentValidation;

using Hangfire;

using Microsoft.AspNetCore.Mvc;

namespace Neba.Api.Legacy.BowlerMemberships;

internal static class DeleteBowlerMembershipEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapDeleteBowlerMembership()
        {
            app.MapPost("/bowler-memberships/delete", (
                DeleteBowlerMembershipRequest request,
                [FromServices] IValidator<DeleteBowlerMembershipRequest> validator,
                [FromServices] IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                // No delay: a delete that beats the add finds nothing, and the add job then finds the Software row gone.
                jobs.Enqueue<BowlerMembershipSyncJob>(job => job.DeleteAsync(request.BowlerMembershipId, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}

internal sealed record DeleteBowlerMembershipRequest(int BowlerMembershipId);

internal sealed class DeleteBowlerMembershipRequestValidator
    : AbstractValidator<DeleteBowlerMembershipRequest>
{
    public DeleteBowlerMembershipRequestValidator()
    {
        RuleFor(request => request.BowlerMembershipId)
            .GreaterThan(0);
    }
}
