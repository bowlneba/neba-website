namespace Neba.Api.Security;

/// <summary>
/// Represents settings for the public-facing website, as distinct from the API itself.
/// </summary>
internal sealed record WebsiteSettings
{
#pragma warning disable S1075 // URIs should not be hardcoded
    private const string PreviewBaseUrl = "https://preview.bowlneba.com";
#pragma warning restore S1075

    /// <summary>
    /// Gets whether the website is currently deployed only to the preview environment
    /// (preview.bowlneba.com). While true, <see cref="BaseUrl"/> is overridden to the preview domain
    /// so links (e.g. invite/reset emails) work without hand-editing the domain everywhere — flip this
    /// off once production launch is ready. Local development (which points <see cref="BaseUrl"/> at
    /// localhost) must set this to false so the override doesn't take over.
    /// </summary>
    public bool InPreview { get; init; }

    /// <summary>
    /// Gets the base URL of the public-facing website. Used to build links (e.g. invite emails) that
    /// point back to the website rather than the API. Do not reuse <see cref="JwtSettings.Audience"/> for
    /// this — Audience is a JWT validation claim, not a website URL, and the two only happen to match in Production.
    /// Overridden by <see cref="PreviewBaseUrl"/> when <see cref="InPreview"/> is true.
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;

    internal WebsiteSettings WithPreviewOverride() =>
        InPreview ? this with { BaseUrl = PreviewBaseUrl } : this;
}