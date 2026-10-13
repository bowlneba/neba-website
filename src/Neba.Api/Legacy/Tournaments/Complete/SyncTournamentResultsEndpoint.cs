using FluentValidation;

using Hangfire;

using Microsoft.AspNetCore.Mvc;

namespace Neba.Api.Legacy.Tournaments.Complete;

// Re-runs only SyncTournamentResultsJob, without the completion and season-stats steps of
// /tournaments/complete. Safe to repeat: results already recorded are skipped, so this adds
// whatever was missing (e.g. a bowler synced after the first run). Reuses CompleteTournamentRequest
// since the payload is the same legacy tournament id.
internal static class SyncTournamentResultsEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapSyncTournamentResults()
        {
            app.MapPost("/tournaments/results/sync", (
                CompleteTournamentRequest request,
                [FromServices] IValidator<CompleteTournamentRequest> validator,
                [FromServices] IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                jobs.Enqueue<SyncTournamentResultsJob>(job => job.SyncAsync(request.TournamentId, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}
