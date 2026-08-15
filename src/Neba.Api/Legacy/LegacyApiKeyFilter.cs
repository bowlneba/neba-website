using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;

namespace Neba.Api.Legacy;

/// <summary>
/// Route-group filter for `/legacy` — checks the <c>X-Api-Key</c> header against the configured
/// shared secret. Deliberately a filter, not an ASP.NET Core AuthenticationScheme: the app already
/// has a default JWT bearer scheme (see SecurityConfiguration), and scoping auth to just this group
/// avoids any interaction with that default.
/// </summary>
internal sealed class LegacyApiKeyFilter(IOptions<LegacySettings> settings, ILogger<LegacyApiKeyFilter> logger) : IEndpointFilter
{
    internal const string ApiKeyHeaderName = "X-Api-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var providedKey = context.HttpContext.Request.Headers[ApiKeyHeaderName].ToString();

        if (!IsValidKey(providedKey, settings.Value.ApiKey))
        {
            logger.LogLegacyApiKeyRejected(context.HttpContext.Request.Path);
            return Results.Unauthorized();
        }

        // Audit trail actor for the Software's sync calls — CurrentUserService.ActorId reads
        // this same claim, so a validated backdoor request is attributed as LegacyActor.Id
        // instead of falling through to "anonymous". The enqueued *SyncJob has no HttpContext,
        // so it separately sets AmbientActorContext with the same id for its own audit events.
        context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, LegacyActor.Id)],
            authenticationType: "LegacyApiKey"));

        return await next(context);
    }

    // Shared with Health.cs, which needs the same fixed-time comparison but must not go through
    // this filter (a health check needs to distinguish "reachable, wrong key" (403) from
    // "unreachable", which this filter's 401 can't express) — both files are deleted together
    // at sunset, so sharing this one comparison avoids the two silently drifting.
    internal static bool IsValidKey(string providedKey, string configuredKey)
    {
        if (string.IsNullOrEmpty(providedKey))
        {
            return false;
        }

        // Fixed-time comparison: a shared API key is a secret worth comparing safely,
        // same reasoning as password/token comparisons elsewhere in the app.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedKey),
            Encoding.UTF8.GetBytes(configuredKey));
    }
}

internal static partial class LegacyApiKeyFilterLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Rejected /legacy request to '{Path}': missing or invalid X-Api-Key header.")]
    public static partial void LogLegacyApiKeyRejected(this ILogger logger, PathString path);
}