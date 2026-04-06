using Neba.Api.Contracts.Sponsors;

namespace Neba.Website.Server.Sponsors;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Static types should be used

internal static class SponsorMappingExtensions
{
    extension(SponsorSummaryResponse response)
    {
        public SponsorSummaryViewModel ToViewModel() =>
            new()
            {
                Name = response.Name,
                Slug = response.Slug,
                LogoUrl = response.LogoUrl,
                IsCurrentSponsor = response.IsCurrentSponsor,
                Priority = response.Priority,
                Tier = response.Tier,
                Category = response.Category,
                TagPhrase = response.TagPhrase,
                Description = response.Description,
                WebsiteUrl = response.WebsiteUrl,
                FacebookUrl = response.FacebookUrl,
                InstagramUrl = response.InstagramUrl
            };
    }
}
