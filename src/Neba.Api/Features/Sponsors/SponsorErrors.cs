using ErrorOr;

namespace Neba.Api.Features.Sponsors;

internal static class SponsorErrors
{
    public static Error SponsorNotFound(string slug)
        => Error.NotFound(
            code: "Sponsor.NotFound",
            description: "Sponsor not found.",
            metadata: new()
            {
                {"slug", slug}
            });
}