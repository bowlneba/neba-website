using Neba.Api.Features.Sponsors.CreateSponsor;
using Neba.Api.Features.Sponsors.Domain;

namespace Neba.TestFactory.Sponsors;

public static class CreatedSponsorFactory
{
    public static CreatedSponsor Create(SponsorId? id = null, string? slug = null)
        => new()
        {
            Id = id ?? SponsorId.New(),
            Slug = slug ?? SponsorFactory.ValidSlug
        };
}
