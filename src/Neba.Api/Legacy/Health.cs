using Microsoft.Extensions.Options;

namespace Neba.Api.Legacy;

internal static class HealthEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        // Deliberately mapped as a standalone top-level route, NOT inside MapLegacyGroup() - the
        // group's LegacyApiKeyFilter would return 401 for a bad key before this handler ever ran,
        // but the Software's health check needs to distinguish "reachable, wrong key" (403) from
        // "unreachable" - so the key check happens explicitly, in here, instead of via the filter.
        public void MapLegacyHealth()
        {
            app.MapGet("/legacy/health", (HttpContext context, IOptions<LegacySettings> settings, ILogger<Program> logger) =>
            {
                var providedKey = context.Request.Headers[LegacyApiKeyFilter.ApiKeyHeaderName].ToString();

                if (!LegacyApiKeyFilter.IsValidKey(providedKey, settings.Value.ApiKey))
                {
                    logger.LogLegacyHealthCheckRejected();
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                return Results.NoContent();
            })
            .AllowAnonymous();
        }
    }
}

internal static partial class HealthEndpointLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Rejected /legacy/health request: missing or invalid X-Api-Key header.")]
    public static partial void LogLegacyHealthCheckRejected(this ILogger<Program> logger);
}