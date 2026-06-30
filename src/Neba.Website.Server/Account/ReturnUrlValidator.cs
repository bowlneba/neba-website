namespace Neba.Website.Server.Account;

internal static class ReturnUrlValidator
{
    // Rejects protocol-relative ("//evil.com") and absolute URLs, which would otherwise let a
    // crafted `?ReturnUrl=` query parameter redirect a logged-in user off-site after login.
    public static string GetSafeReturnUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//", StringComparison.Ordinal))
            return returnUrl;

        return "/";
    }
}
