using FluentValidation;

using Hangfire;

using Microsoft.AspNetCore.Mvc;

using Neba.Api.Auditing;

namespace Neba.Api.Legacy.Seasons.Complete;

internal static class CompleteSeasonEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapCompleteSeason()
        {
            app.MapPost("/seasons/complete", (
                CompleteSeasonRequest request,
                HttpContext httpContext,
                [FromServices] IValidator<CompleteSeasonRequest> validator,
                [FromServices] IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                var correlationId = AmbientCorrelationContext.Capture(httpContext);
                jobs.Enqueue<CompleteSeasonSyncJob>(job => job.SyncAsync(request.SeasonId, correlationId, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}