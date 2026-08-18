using FluentValidation;

using Hangfire;

namespace Neba.Api.Legacy.Tournaments.Complete;

internal static class CompleteTournamentEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapCompleteTournament()
        {
            app.MapPost("/tournaments/complete", (
                CompleteTournamentRequest request,
                IValidator<CompleteTournamentRequest> validator,
                IBackgroundJobClient jobs) =>
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
