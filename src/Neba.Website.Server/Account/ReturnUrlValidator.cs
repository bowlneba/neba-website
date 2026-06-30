namespace Neba.Website.Server.Account;

internal static class ReturnUrlValidator
{
    // Rejects protocol-relative ("//evil.com"), backslash-based ("\/evil.com"), and absolute URLs,
    // which would otherwise let a crafted `?ReturnUrl=` redirect a logged-in user off-site after login.
    // Leading "/\" is normalized to "//" by some browsers and treated as protocol-relative.
    public static string GetSafeReturnUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl)
            && returnUrl.StartsWith('/')
            && !returnUrl.StartsWith("//", StringComparison.Ordinal)
            && !returnUrl.Contains('\\', StringComparison.Ordinal))
            return returnUrl;

        return "/";
    }
}