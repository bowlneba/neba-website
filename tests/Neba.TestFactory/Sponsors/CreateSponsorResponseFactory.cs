using Neba.Api.Contracts.Sponsors.CreateSponsor;

namespace Neba.TestFactory.Sponsors;

public static class CreateSponsorResponseFactory
{
    public static SponsorResponse Create(string? sponsorId = null, string? slug = null)
        => new()
        {
            SponsorId = sponsorId ?? "01J7ZK8X6ZQJ8V3F8N9T9C9R2E",
            Slug = slug ?? SponsorFactory.ValidSlug
        };
}
