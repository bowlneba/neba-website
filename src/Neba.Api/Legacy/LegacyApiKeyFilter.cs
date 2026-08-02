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
internal sealed class LegacyApiKeyFilter(IOptions<LegacySettings> settings) : IEndpointFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var providedKey = context.HttpContext.Request.Headers[ApiKeyHeaderName].ToString();

        return !IsValidKey(providedKey) 
            ? Results.Unauthorized() 
            : await next(context);

    }

    private bool IsValidKey(string providedKey)
    {
        if (string.IsNullOrEmpty(providedKey))
        {
            return false;
        }

        // Fixed-time comparison: a shared API key is a secret worth comparing safely,
        // same reasoning as password/token comparisons elsewhere in the app.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedKey),
            Encoding.UTF8.GetBytes(settings.Value.ApiKey));
    }
}