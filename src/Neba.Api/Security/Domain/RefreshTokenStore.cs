using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Identity;

namespace Neba.Api.Security.Domain;

// Centralizes the refresh-token storage format and Identity token-provider/name pair so
// Login, RefreshToken, and Logout handlers stay in sync if the format or naming changes.
internal static class RefreshTokenStore
{
    public const string Provider = "RefreshToken";
    public const string Name = "RefreshToken";

    public static Task StoreAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string rawToken,
        TimeProvider timeProvider)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var stored = new StoredRefreshToken
        {
            Hash = hash,
            IssuedAt = timeProvider.GetUtcNow()
        };
        var json = JsonSerializer.Serialize(stored);

        return userManager.SetAuthenticationTokenAsync(user, Provider, Name, json);
    }

    public static Task<string?> GetStoredJsonAsync(UserManager<ApplicationUser> userManager, ApplicationUser user)
        => userManager.GetAuthenticationTokenAsync(user, Provider, Name);

    public static Task RemoveAsync(UserManager<ApplicationUser> userManager, ApplicationUser user)
        => userManager.RemoveAuthenticationTokenAsync(user, Provider, Name);
}
