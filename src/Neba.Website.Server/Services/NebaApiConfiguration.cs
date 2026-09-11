namespace Neba.Website.Server.Services;

internal sealed record NebaApiConfiguration
{
    public required Uri BaseUrl { get; init; }

    // The API's public production host (api.bowlneba.com — see AccountConfiguration.
    // SharedAuthCookieDomain for the matching parent-domain cookie setup). BaseUrl above is
    // Aspire's internal service-discovery address for server-to-server Refit calls, not this
    // public host, so it can't be reused to build browser-facing links (e.g. the Background
    // Jobs dashboard link in AccountMenu.razor).
#pragma warning disable S1075 // URIs should not be hardcoded
    public const string PublicBaseUrl = "https://api.bowlneba.com";
#pragma warning restore S1075
}