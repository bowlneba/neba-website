using FluentValidation;

using Hangfire;

using Microsoft.AspNetCore.Mvc;

namespace Neba.Api.Legacy.Tournaments.Complete;

internal static class CompleteTournamentEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapCompleteTournament()
        {
            app.MapPost("/tournaments/complete", (
                CompleteTournamentRequest request,
                [FromServices] IValidator<CompleteTournamentRequest> validator,
                [FromServices] IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                jobs.Enqueue<CompleteTournamentSyncJob>(job => job.SyncAsync(request.TournamentId, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}