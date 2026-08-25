using FluentValidation;

using Hangfire;

using Microsoft.AspNetCore.Mvc;

namespace Neba.Api.Legacy.Seasons.Complete;

internal static class CompleteSeasonEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapCompleteSeason()
        {
            app.MapPost("/seasons/complete", (
                CompleteSeasonRequest request,
                [FromServices] IValidator<CompleteSeasonRequest> validator,
                [FromServices] IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                jobs.Enqueue<CompleteSeasonSyncJob>(job => job.SyncAsync(request.SeasonId, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}