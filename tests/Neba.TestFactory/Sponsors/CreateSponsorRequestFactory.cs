using Neba.Api.Contracts.Sponsors.CreateSponsor;

namespace Neba.TestFactory.Sponsors;

public static class CreateSponsorRequestFactory
{
    public static CreateSponsorRequest Create(SponsorInput? sponsor = null)
        => new()
        {
            Sponsor = sponsor ?? SponsorInputFactory.Create()
        };
}
