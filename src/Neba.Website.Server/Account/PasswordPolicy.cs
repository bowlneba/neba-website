namespace Neba.Website.Server.Account;

/// <summary>
/// Mirrors the Identity password policy configured in <c>SecurityConfiguration.AddSecurity()</c>
/// (<c>Neba.Api</c>, internal, unreachable here). This is a client-side hint only — the API endpoint
/// remains the actual enforcement point. Keep in sync if the server-side policy changes.
/// </summary>
internal static class PasswordPolicy
{
    public const int RequiredLength = 8;
    public const bool RequireDigit = true;
    public const bool RequireUppercase = true;
    public const bool RequireLowercase = true;
}
