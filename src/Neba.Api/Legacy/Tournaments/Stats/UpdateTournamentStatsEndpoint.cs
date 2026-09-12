using FluentValidation;

using Hangfire;

using Microsoft.AspNetCore.Mvc;

using Neba.Api.Auditing;

namespace Neba.Api.Legacy.Tournaments.Stats;

internal static class UpdateTournamentStatsEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapUpdateTournamentStats()
        {
            app.MapPost("/tournaments/stats/update", (
                UpdateTournamentStatsRequest request,
                HttpContext httpContext,
                [FromServices] IValidator<UpdateTournamentStatsRequest> validator,
                [FromServices] IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                var correlationId = AmbientCorrelationContext.Capture(httpContext);
                jobs.Enqueue<GenerateSeasonStatsJob>(job => job.SyncAsync(request.TournamentId, correlationId, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}