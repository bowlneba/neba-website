using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.AspNetCore.DataProtection;

namespace Neba.Website.Server.Account.Session;

/// <summary>
/// Encrypts session tokens so they can pass through the browser between a Blazor circuit and the
/// session-token endpoints without the browser being able to read them.
/// </summary>
internal sealed class SessionTokenProtector(IDataProtectionProvider dataProtectionProvider, ILogger<SessionTokenProtector> logger)
{
    private const string Purpose = "Neba.Website.Account.SessionTokens.v1";

    // Long enough for one browser round trip, short enough that a captured blob soon goes stale.
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);

    private readonly ITimeLimitedDataProtector _protector = dataProtectionProvider
        .CreateProtector(Purpose)
        .ToTimeLimitedDataProtector();

    public string Protect(SessionTokens tokens)
        => _protector.Protect(JsonSerializer.Serialize(tokens), Lifetime);

    /// <summary>Returns null when the blob is expired, tampered with, or malformed.</summary>
    public SessionTokens? Unprotect(string blob)
    {
        try
        {
            return JsonSerializer.Deserialize<SessionTokens>(_protector.Unprotect(blob));
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException)
        {
            logger.LogSessionTokenBlobRejected(ex);
            return null;
        }
    }
}

internal static partial class SessionTokenProtectorLogMessages
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Rejected a session token blob that was expired, tampered with, or malformed.")]
    public static partial void LogSessionTokenBlobRejected(this ILogger<SessionTokenProtector> logger, Exception exception);
}