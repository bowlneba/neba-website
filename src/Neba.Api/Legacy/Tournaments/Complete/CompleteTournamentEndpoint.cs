using FluentValidation;

using Hangfire;

using Microsoft.AspNetCore.Mvc;

using Neba.Api.Auditing;

namespace Neba.Api.Legacy.Tournaments.Complete;

internal static class CompleteTournamentEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapCompleteTournament()
        {
            app.MapPost("/tournaments/complete", (
                CompleteTournamentRequest request,
                HttpContext httpContext,
                [FromServices] IValidator<CompleteTournamentRequest> validator,
                [FromServices] IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                var correlationId = AmbientCorrelationContext.Capture(httpContext);
                jobs.Enqueue<CompleteTournamentSyncJob>(job => job.SyncAsync(request.TournamentId, correlationId, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}