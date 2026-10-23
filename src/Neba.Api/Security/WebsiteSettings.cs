namespace Neba.Api.Security;

/// <summary>
/// Represents settings for the public-facing website, as distinct from the API itself.
/// </summary>
internal sealed record WebsiteSettings
{
    /// <summary>
    /// Gets the base URL of the public-facing website. Used to build links (e.g. invite emails) that
    /// point back to the website rather than the API. Do not reuse <see cref="JwtSettings.Audience"/> for
    /// this — Audience is a JWT validation claim, not a website URL, and the two only happen to match in Production.
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;
}