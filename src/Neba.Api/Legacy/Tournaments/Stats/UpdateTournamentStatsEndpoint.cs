using FluentValidation;

using Hangfire;

using Microsoft.AspNetCore.Mvc;

namespace Neba.Api.Legacy.Tournaments.Stats;

internal static class UpdateTournamentStatsEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapUpdateTournamentStats()
        {
            app.MapPost("/tournaments/stats/update", (
                UpdateTournamentStatsRequest request,
                [FromServices] IValidator<UpdateTournamentStatsRequest> validator,
                [FromServices] IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                jobs.Enqueue<GenerateSeasonStatsJob>(job => job.SyncAsync(request.TournamentId, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}